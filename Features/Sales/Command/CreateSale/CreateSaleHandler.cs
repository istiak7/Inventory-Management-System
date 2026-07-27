using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);
            if (customer == null)
                return Error(404, "Customer not found.");

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
                var saleDate = request.SaleDate ?? DateTime.UtcNow;

                var invoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
                    ? await GenerateInvoiceNumberAsync(saleDate, cancellationToken)
                    : request.InvoiceNumber.Trim();

                var sale = new CustomerSale
                {
                    CustomerId = request.CustomerId,
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

                    // Price comes from the catalog, never from the client (price-manipulation guard).
                    var unitPrice = variant.SellingPrice;
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
                        TransactionType = InventoryTxnType.SaleOut,
                        QuantityIn = 0,
                        QuantityOut = item.Quantity,
                        BalanceAfter = newBalance,
                        TransactionDate = saleDate,
                        Branch = branch,
                        ProductVariant = variant,
                    }, cancellationToken);

                    sale.SaleDetails.Add(new SaleDetails
                    {
                        ProductVariantId = variant.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        DiscountPerItem = item.DiscountPerItem,
                        TotalAmount = lineTotal,
                        WarrantyMonths = item.WarrantyMonths,
                        Status = SaleLineStatus.Completed,
                        CustomerSale = sale,
                        ProductVariant = variant,
                    });

                    itemResponses.Add(new SaleItemResponse(
                        variant.Id, variant.Product.ProductName, item.Quantity, unitPrice, lineTotal, item.WarrantyMonths));
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
                    runningBalance -= paidAmount;
                    await _dbContext.CustomerTransactions.AddAsync(new CustomerTransaction
                    {
                        CustomerId = sale.CustomerId,
                        TransactionType = "Payment",
                        TransactionDate = payment.PaymentDate,
                        Debit = paidAmount,
                        Credit = 0,
                        BalanceAfter = runningBalance,
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
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating sale for customer {CustomerId}", request.CustomerId);
                return Error(500, "An error occurred while creating the sale.");
            }
        }

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
