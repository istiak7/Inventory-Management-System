using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Customers.Shared;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
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
            #region Basic validation

            if (request.Items.Count == 0)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "At least one item is required to create a sale."
                };

            var branch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Branch not found."
                };

            // Client invoice numbers must not same with an existing one.
            if (!string.IsNullOrWhiteSpace(request.InvoiceNumber))
            {
                var taken = await _dbContext.CustomerSales
                    .AnyAsync(s => s.InvoiceNumber == request.InvoiceNumber, cancellationToken);
                if (taken)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Invoice number '{request.InvoiceNumber}' already exists."
                    };
            }

            #endregion

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
                            return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"Product variant with id {item.ProductVariantId} not found." };

                        variant = loaded;
                        variantCache[item.ProductVariantId] = variant;
                    }

                    // Stock is per (Branch, Variant) — a sale can only draw from its own branch.
                    if (!stockCache.TryGetValue(variant.Id, out var stock))
                    {
                        var loaded = await _dbContext.Stocks
                            .FirstOrDefaultAsync(s => s.BranchId == request.BranchId && s.ProductVariantId == variant.Id, cancellationToken);
                        if (loaded == null)
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}): none on hand at this branch." };

                        stock = loaded;
                        stockCache[variant.Id] = stock;
                    }

                    if (stock.CurrentStock < item.Quantity)
                        return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}): {stock.CurrentStock} on hand, {item.Quantity} requested." };

                    // Serialized variants sell one physical unit per line — the serial IS the unit,
                    // so a quantity stepper would let the client claim units it never named.
                    ProductSerial? serial = null;
                    if (variant.IsSerialized)
                    {
                        if (item.Quantity != 1)
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"'{variant.Product.ProductName}' is serialized: each line sells exactly 1 unit. Add another line for additional units." };

                        var serialNumber = item.SerialNumber?.Trim();
                        if (string.IsNullOrEmpty(serialNumber))
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"A serial number is required for '{variant.Product.ProductName}' (SKU {variant.SKU})." };

                        serial = await _dbContext.ProductSerials.FirstOrDefaultAsync(s =>
                            s.SerialNumber == serialNumber &&
                            s.ProductVariantId == variant.Id &&
                            s.BranchId == request.BranchId &&
                            s.Status == SerialStatus.InStock, cancellationToken);

                        if (serial == null || !claimedSerialIds.Add(serial.Id))
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Serial number '{serialNumber}' is not available in stock for '{variant.Product.ProductName}' at this branch." };
                    }

                    // Price comes from the catalog, never from the client (price-manipulation guard).
                    var unitPrice = variant.SellingPrice;


                    var warrantyMonths = serial?.WarrantyMonths ?? item.WarrantyMonths;

                    var discountPerItem = item.DiscountPerItem ?? 0;
                    if (discountPerItem > unitPrice)
                        return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"DiscountPerItem {discountPerItem} exceeds the unit price {unitPrice} for '{variant.Product.ProductName}'." };

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
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"DiscountAmount {request.DiscountAmount} exceeds the subtotal {subTotal}." };

                var totalAmount = subTotal - request.DiscountAmount + request.TaxAmount;

                // Payment mode inferred from Amount vs total (no silent clamping).
                var paidAmount = request.Payment?.Amount ?? 0;
                if (paidAmount < 0)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Payment amount cannot be negative." };
                if (paidAmount > totalAmount)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Payment exceeds the total. Pay the full amount or a smaller one." };

                sale.SubTotal = subTotal;
                sale.DiscountAmount = request.DiscountAmount;
                sale.TaxAmount = request.TaxAmount;
                sale.TotalAmount = totalAmount;
                sale.PaidAmount = 0;
                sale.DueAmount = totalAmount;
                sale.SaleType = paidAmount >= totalAmount && totalAmount > 0 ? SaleType.Cash : SaleType.Credit;

                await _dbContext.CustomerSales.AddAsync(sale, cancellationToken);

                // Ledger (money): the sale credits the customer account (they owe us more).
                var runningBalance = await _dbContext.CustomerTransactions
                    .Where(t => t.CustomerId == sale.CustomerId)
                    .GetLatestBalanceAsync(cancellationToken);
                runningBalance += totalAmount;

                var customerTransaction = CustomerTransaction.ForSale(sale, customer, runningBalance);
                await _dbContext.CustomerTransactions.AddAsync(customerTransaction, cancellationToken);

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
                    customerTransaction = CustomerTransaction.ForPayment(payment, customer, runningBalance, sale);
                    await _dbContext.CustomerTransactions.AddAsync(customerTransaction, cancellationToken);
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
                    "IX_SaleDetails_ProductSerialId" => new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = "One of those serial numbers was sold on another sale a moment ago. Re-scan the unit and try again." },
                    "IX_CustomerSales_InvoiceNumber" => new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = "That invoice number was taken by another sale a moment ago. Leave it blank to have one generated." },
                    _ => new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = "This sale clashed with another one saved at the same moment. Nothing was recorded — please try again." },
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating sale for customer {CustomerId} / phone {PhoneNumber}",
                    request.CustomerId, request.Customer?.PhoneNumber);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while creating the sale." };
            }
        }

        private async Task<(Customer? Customer, Result? Error)> ResolveCustomerAsync(
            CreateSaleCommand request, CancellationToken cancellationToken)
        {
            if (request.CustomerId is > 0)
            {
                var existing = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

                return existing == null
                    ? (null, new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Customer not found." })
                    : (existing, null);
            }

            if (request.Customer == null)
                return (null, new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Either CustomerId or Customer details are required." });

            var phoneNumber = CustomerPhoneNumber.Normalize(request.Customer.PhoneNumber);
            if (phoneNumber.Length < CustomerPhoneNumber.MinimumDigits)
                return (null, new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Customer phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits." });

            // The number is the identity, so an existing customer
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
                OpeningBalance = 0,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.Customers.AddAsync(customer, cancellationToken);

            var transaction = _dbContext.Database.CurrentTransaction;
            const string savepoint = "before_customer_insert";
            if (transaction != null)
                await transaction.CreateSavepointAsync(savepoint, cancellationToken);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return (customer, null);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                if (transaction != null)
                    await transaction.RollbackToSavepointAsync(savepoint, cancellationToken);

                // The insert is undone but EF still has it pending — drop it, or the sale's own
                // SaveChanges would replay it and hit the same constraint.
                _dbContext.Entry(customer).State = EntityState.Detached; // ef core memory tracking: forget the failed insert

                var winner = await _dbContext.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber, cancellationToken);

                return winner != null
                    ? (winner, null)
                    : (null, new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "Could not register the customer for this sale." });
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

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
    }
}
