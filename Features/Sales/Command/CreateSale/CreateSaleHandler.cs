using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Inventory_Management_System.Features.Sales.Command.CreateSale
{
    // POS-style sale (Approach 1): the sale, the stock movement, the invoice and any payment are
    // all committed in ONE transaction. There is no pending/delivery stage — goods leave the
    // counter as the row is written, so Status is Completed from birth.
    public class CreateSaleHandler(
            AppDbContext _dbContext,
            ILogger<CreateSaleHandler> _logger
        ) : IRequestHandler<CreateSaleCommand, Result>
    {
        public async Task<Result> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
        {
            if (request.Items.Count == 0)
                return Error(400, "At least one sale item is required.");

            var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return Error(404, "Branch not found.");

            // Client-supplied invoice numbers must not collide with an existing one.
            if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
            {
                var taken = await _dbContext.CustomerSales
                    .AnyAsync(s => s.InvoiceNumber == request.InvoiceNumber, cancellationToken);
                if (taken)
                    return Error(400, $"Invoice number '{request.InvoiceNumber}' already exists.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Inside the transaction on purpose: when the sales form supplies a new customer,
                // registering them and ringing up the sale succeed or fail together. A sale that
                // dies on insufficient stock must not leave a customer behind.
                var resolved = await ResolveCustomerAsync(request, cancellationToken);
                if (resolved.Error != null)
                    return resolved.Error;
                var customer = resolved.Customer!;   // non-null whenever Error is null

                var saleDate = request.SaleDate ?? DateTime.UtcNow;

                var invoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                    ? await GenerateInvoiceNumberAsync(saleDate, cancellationToken)
                    : request.InvoiceNumber.Trim();

                var sale = new CustomerSale
                {
                    CustomerId = customer.Id,
                    BranchId = request.BranchId,
                    SaleDate = saleDate,
                    InvoiceNumber = invoiceNumber,
                    Status = SaleStatus.Completed,   // POS: nothing to approve, nothing to deliver
                    Customer = customer,
                    Branch = branch,
                };

                // Same variant may appear on several lines — cache both the variant and its stock
                // row so quantities accumulate against ONE snapshot instead of each line re-reading
                // the pre-sale on-hand and passing a check it should have failed.
                var variantCache = new Dictionary<int, ProductVariant>();
                var stockCache = new Dictionary<int, Stock>();
                var itemResponses = new List<SaleItemResponse>();
                // Guards the same physical unit from being sold on two lines of the same request —
                // the per-row DB check alone can't see a serial this same loop already claimed.
                var claimedSerialIds = new HashSet<int>();

                decimal subTotal = 0;
                foreach (var item in request.Items)
                {
                    if (!variantCache.TryGetValue(item.ProductVariantId, out var variant))
                    {
                        var loaded = await _dbContext.ProductVariants
                            .Include(v => v.Product)
                            .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, cancellationToken);
                        if (loaded == null)
                            return Error(404, $"Product variant with id {item.ProductVariantId} not found.");

                        variant = loaded;
                        variantCache[item.ProductVariantId] = variant;
                    }

                    // Stock is per (Branch, Variant) — a sale can only draw from its own branch.
                    if (!stockCache.TryGetValue(variant.Id, out var stock))
                    {
                        var loaded = await _dbContext.Stocks
                            .FirstOrDefaultAsync(s => s.BranchId == request.BranchId && s.ProductVariantId == variant.Id, cancellationToken);
                        if (loaded == null)
                            return Error(400, $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}): none on hand at this branch.");

                        stock = loaded;
                        stockCache[variant.Id] = stock;
                    }

                    if (stock.CurrentStock < item.Quantity)
                        return Error(400, $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}): {stock.CurrentStock} on hand, {item.Quantity} requested.");

                    // Serialized variants sell one physical unit per line — the serial IS the unit,
                    // so a quantity stepper would let the client claim units it never named.
                    ProductSerial? serial = null;
                    if (variant.IsSerialized)
                    {
                        if (item.Quantity != 1)
                            return Error(400, $"'{variant.Product.ProductName}' is serialized: each line sells exactly 1 unit. Add another line for additional units.");

                        var serialNumber = item.SerialNumber?.Trim();
                        if (string.IsNullOrEmpty(serialNumber))
                            return Error(400, $"A serial number is required for '{variant.Product.ProductName}' (SKU {variant.SKU}).");

                        serial = await _dbContext.ProductSerials.FirstOrDefaultAsync(s =>
                            s.SerialNumber == serialNumber &&
                            s.ProductVariantId == variant.Id &&
                            s.BranchId == request.BranchId &&
                            s.Status == SerialStatus.InStock, cancellationToken);

                        if (serial == null || !claimedSerialIds.Add(serial.Id))
                            return Error(400, $"Serial number '{serialNumber}' is not available in stock for '{variant.Product.ProductName}' at this branch.");
                    }

                    // Price comes from the catalog, never from the client (price-manipulation guard).
                    var unitPrice = variant.SellingPrice;

                    // Warranty is resolved the same way for a serialized unit: ProductSerial.WarrantyMonths
                    // was copied from its purchase lot at receipt, so it IS the term this physical unit
                    // carries and the client cannot talk it up. Pooled non-serialized stock has no single
                    // lot to read from, so there the line's own value stands.
                    var warrantyMonths = serial?.WarrantyMonths ?? item.WarrantyMonths;

                    var discountPerItem = item.DiscountPerItem ?? 0;
                    if (discountPerItem > unitPrice)
                        return Error(400, $"DiscountPerItem {discountPerItem} exceeds the unit price {unitPrice} for '{variant.Product.ProductName}'.");

                    var lineTotal = (unitPrice - discountPerItem) * item.Quantity;
                    subTotal += lineTotal;

                    // Draw down the snapshot and append the matching ledger row, so
                    // Stock.CurrentStock stays the derived value the ledger says it is.
                    var newBalance = stock.CurrentStock - item.Quantity;
                    stock.CurrentStock = newBalance;

                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = request.BranchId,
                        ProductVariantId = variant.Id,
                        // Cost lineage. A serialized unit knows the lot it arrived in, so the
                        // SaleOut row can name it and its cost (lot.UnitPrice) is recoverable —
                        // that is what makes exact COGS/profit reportable per line. A
                        // non-serialized sale draws from pooled stock with no single lot, so it
                        // stays null rather than guessing one.
                        SupplierPurchaseDetailsId = serial?.SupplierPurchaseDetailsId,
                        TransactionType = InventoryTxnType.SaleOut,
                        QuantityIn = 0,
                        QuantityOut = item.Quantity,
                        BalanceAfter = newBalance,
                        TransactionDate = saleDate,
                        Branch = branch,
                        ProductVariant = variant,
                    }, cancellationToken);

                    // The unit is leaving the branch as this sale is written, so its lifecycle
                    // closes here — never recomputed or left InStock for a sale that just sold it.
                    if (serial != null)
                    {
                        serial.Status = SerialStatus.Sold;
                        serial.SoldDate = saleDate;
                    }

                    sale.SaleDetails.Add(new SaleDetails
                    {
                        ProductVariantId = variant.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        DiscountPerItem = item.DiscountPerItem,
                        TotalAmount = lineTotal,
                        WarrantyMonths = warrantyMonths,
                        ProductSerialId = serial?.Id,
                        Status = SaleLineStatus.Completed,
                        CustomerSale = sale,
                        ProductVariant = variant,
                        ProductSerial = serial,
                    });

                    itemResponses.Add(new SaleItemResponse(
                        variant.Id, variant.Product.ProductName, item.Quantity, unitPrice, lineTotal, warrantyMonths, serial?.SerialNumber));
                }

                if (request.DiscountAmount > subTotal)
                    return Error(400, $"DiscountAmount {request.DiscountAmount} exceeds the subtotal {subTotal}.");

                var totalAmount = subTotal - request.DiscountAmount + request.TaxAmount;

                // Payment mode inferred from Amount vs total (no silent clamping).
                var paidAmount = request.Payment?.Amount ?? 0;
                if (paidAmount < 0)
                    return Error(400, "Payment amount cannot be negative.");
                if (paidAmount > totalAmount)
                    return Error(400, "Payment exceeds the total. Pay the full amount or a smaller one.");

                sale.SubTotal = subTotal;
                sale.DiscountAmount = request.DiscountAmount;
                sale.TaxAmount = request.TaxAmount;
                sale.TotalAmount = totalAmount;
                sale.PaidAmount = 0;
                sale.DueAmount = totalAmount;
                sale.SaleType = paidAmount >= totalAmount && totalAmount > 0 ? SaleType.Cash : SaleType.Credit;

                await _dbContext.CustomerSales.AddAsync(sale, cancellationToken);

                // Ledger (money): the sale credits the customer account (they owe us more).
                var runningBalance = await GetCurrentCustomerBalanceAsync(sale.CustomerId, cancellationToken);
                runningBalance += totalAmount;
                await _dbContext.CustomerTransactions.AddAsync(new CustomerTransaction
                {
                    CustomerId = sale.CustomerId,
                    TransactionType = "Sale",
                    TransactionDate = sale.SaleDate,
                    Debit = 0,
                    Credit = totalAmount,
                    BalanceAfter = runningBalance,
                    CustomerSale = sale,
                    Customer = customer,
                }, cancellationToken);

                CustomerPayment? payment = null;
                if (paidAmount > 0)
                {
                    sale.ApplyPayment(paidAmount);   // updates Paid/Due consistently

                    payment = new CustomerPayment
                    {
                        CustomerId = sale.CustomerId,
                        BranchId = sale.BranchId,
                        Amount = paidAmount,
                        PaymentDate = request.Payment?.PaymentDate ?? DateTime.UtcNow,
                        PaymentMethod = request.Payment?.PaymentMethod ?? "Cash",
                        Customer = customer,
                        Branch = branch,
                    };
                    await _dbContext.CustomerPayments.AddAsync(payment, cancellationToken);

                    // This payment settles this sale (manual 1:1 pairing at creation).
                    await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                    {
                        Amount = paidAmount,
                        AllocationDate = payment.PaymentDate,
                        CustomerSale = sale,
                        CustomerPayment = payment,
                    }, cancellationToken);

                    // Ledger (money): the payment debits the customer account (they owe us less).
                    // SaleId is carried as well as CustomerPaymentId: at the counter a payment is
                    // raised against exactly one invoice, so naming it here lets a customer ledger
                    // show what each payment settled without joining back through SaleCustomerPayments.
                    runningBalance -= paidAmount;
                    await _dbContext.CustomerTransactions.AddAsync(new CustomerTransaction
                    {
                        CustomerId = sale.CustomerId,
                        TransactionType = "Payment",
                        TransactionDate = payment.PaymentDate,
                        Debit = paidAmount,
                        Credit = 0,
                        BalanceAfter = runningBalance,
                        CustomerSale = sale,
                        CustomerPayment = payment,
                        Customer = customer,
                    }, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new SaleResponse(
                    sale.Id, sale.CustomerId, sale.BranchId, sale.SaleDate, sale.InvoiceNumber,
                    sale.Status.ToString(), sale.SaleType.ToString(),
                    sale.SubTotal, sale.DiscountAmount, sale.TaxAmount, sale.TotalAmount,
                    sale.PaidAmount, sale.DueAmount,
                    itemResponses,
                    payment == null ? null : new SalePaymentResponse(payment.Id, payment.Amount, payment.PaymentDate, payment.PaymentMethod)
                );

                return new Result { IsSuccess = true, StatusCode = 201, Status = "Success", Message = "Sale created successfully", Data = response };
            }
            // The serial and the invoice number are both guarded by unique indexes, because the
            // pre-checks above cannot see a sale another till is committing right now. Losing that
            // race lands here, so name what actually collided — a bare "something went wrong" sends
            // the counter hunting for a fault that is not theirs.
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                var constraint = (ex.InnerException as PostgresException)?.ConstraintName;
                _logger.LogWarning(ex, "Sale creation lost a uniqueness race on {Constraint}", constraint);

                return constraint switch
                {
                    "IX_SaleDetails_ProductSerialId" => Error(409,
                        "One of those serial numbers was sold on another sale a moment ago. Re-scan the unit and try again."),
                    "IX_CustomerSales_InvoiceNumber" => Error(409,
                        "That invoice number was taken by another sale a moment ago. Leave it blank to have one generated."),
                    _ => Error(409,
                        "This sale clashed with another one saved at the same moment. Nothing was recorded — please try again."),
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating sale for customer {CustomerId} / phone {PhoneNumber}",
                    request.CustomerId, request.Customer?.PhoneNumber);
                return Error(500, "An error occurred while creating the sale.");
            }
        }

        /// <summary>
        /// Who this sale is billed to. Either the customer whose id the form sent, or — when the
        /// form typed a mobile number that its lookup did not recognise — the customer that number
        /// belongs to, registering them if they are genuinely new.
        ///
        /// This is a find-or-create rather than a create: the form's lookup and its save are two
        /// round trips, and the same walk-in can be registered at another till in between. Losing
        /// that race is normal, not exceptional, so both the pre-check and the unique-index
        /// violation resolve the same way — bill the sale to whoever now owns the number.
        /// </summary>
        private async Task<(Customer? Customer, Result? Error)> ResolveCustomerAsync(
            CreateSaleCommand request, CancellationToken cancellationToken)
        {
            if (request.CustomerId is > 0)
            {
                var existing = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

                return existing == null
                    ? (null, Error(404, "Customer not found."))
                    : (existing, null);
            }

            if (request.Customer == null)
                return (null, Error(400, "Either CustomerId or Customer details are required."));

            var phoneNumber = CustomerPhoneNumber.Normalize(request.Customer.PhoneNumber);
            if (phoneNumber.Length < CustomerPhoneNumber.MinimumDigits)
                return (null, Error(400, $"Customer phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits."));

            // The number is the identity, so an existing owner wins outright — the details typed at
            // the till never overwrite a record that is already on file.
            var byPhone = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);
            if (byPhone != null)
                return (byPhone, null);

            var customer = new Customer
            {
                Group = request.Customer.Group,
                Name = request.Customer.Name.Trim(),
                Description = request.Customer.Description,
                PhoneNumber = phoneNumber,
                Email = request.Customer.Email,
                Address = request.Customer.Address,
                NID = request.Customer.NID,
                OpeningBalance = 0,   // met at the counter — they start owing nothing
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.Customers.AddAsync(customer, cancellationToken);

            // Postgres aborts the entire transaction on a constraint violation — every later
            // command fails with 25P02 until it is unwound. A savepoint scopes the damage to just
            // this insert, so losing the race below is still recoverable and the sale can go on.
            var transaction = _dbContext.Database.CurrentTransaction;
            const string savepoint = "before_customer_insert";
            if (transaction != null)
                await transaction.CreateSavepointAsync(savepoint, cancellationToken);

            try
            {
                // Flushed now so customer.Id exists for the sale, its ledger row and any payment.
                // Still inside the caller's transaction, so a later failure rolls this back too.
                await _dbContext.SaveChangesAsync(cancellationToken);
                return (customer, null);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Someone registered this number between the check above and this insert. The
                // unique index did its job; adopt their row instead of failing the sale.
                if (transaction != null)
                    await transaction.RollbackToSavepointAsync(savepoint, cancellationToken);

                // The insert is undone but EF still has it pending — drop it, or the sale's own
                // SaveChanges would replay it and hit the same constraint.
                _dbContext.Entry(customer).State = EntityState.Detached;

                var winner = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

                return winner != null
                    ? (winner, null)
                    : (null, Error(500, "Could not register the customer for this sale."));
            }
        }

        /// <summary>Postgres reports a unique-index breach as SQLSTATE 23505.</summary>
        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        /// <summary>
        /// Next invoice number for the sale's year, as INV-{year}-0001. Ordering by Id (not by the
        /// string) keeps this correct past 9999, where zero-padded text would sort "10000" before
        /// "9999". Two concurrent sales can still race to the same number; the unique index on
        /// InvoiceNumber is the real guard and turns that into a rollback rather than a duplicate.
        /// </summary>
        private async Task<string> GenerateInvoiceNumberAsync(DateTime saleDate, CancellationToken cancellationToken)
        {
            var prefix = $"INV-{saleDate.Year}-";

            var latest = await _dbContext.CustomerSales
                .AsNoTracking()
                .Where(s => s.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(s => s.Id)
                .Select(s => s.InvoiceNumber)
                .FirstOrDefaultAsync(cancellationToken);

            var next = 1;
            if (latest != null && int.TryParse(latest[prefix.Length..], out var lastSequence))
                next = lastSequence + 1;

            return prefix + next.ToString("D4");
        }

        /// <summary>Customer's current receivable = BalanceAfter of their latest transaction (0 if none).</summary>
        private async Task<decimal> GetCurrentCustomerBalanceAsync(int customerId, CancellationToken cancellationToken)
        {
            return await _dbContext.CustomerTransactions
                .AsNoTracking()
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.Id)
                .Select(t => t.BalanceAfter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static Result Error(int code, string message) =>
            new() { IsSuccess = false, StatusCode = code, Status = "Error", Message = message };
    }
}
