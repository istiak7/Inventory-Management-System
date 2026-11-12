using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Parties.Shared;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Features.SalesReturns.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.SalesReturns.Command.CreateSalesReturn
{
    // Takes some lines (some quantity of each) of one sale back from the customer.
    //
    // Stock: restocked lines come back in (ReturnIn, serial InStock); damaged lines do not
    // (serial Defective).
    // Money, for return worth R (the lines' share of the sale total, discount and tax included):
    //   1. what is still due on the sale is reduced first, up to R;
    //   2. the rest was already paid: a part paid by a cross-ledger Adjustment is reversed
    //      (we owe the party as a supplier again, no cash moves);
    //   3. anything still left is refunded in cash (a negative "Refund" payment, like Void).
    // The sale's TotalAmount stays; its ReturnedAmount grows by R.
    public class CreateSalesReturnHandler(
        AppDbContext _dbContext,
        ILogger<CreateSalesReturnHandler> _logger
    ) : IRequestHandler<CreateSalesReturnCommand, Result>
    {
        public async Task<Result> Handle(CreateSalesReturnCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Document first (lock order: document -> stock -> customer -> supplier). Two returns
                // on the same sale now take turns, so the remaining quantities are always current.
                await _dbContext.LockRowAsync<CustomerSale>(request.SaleId, cancellationToken);
                var sale = await _dbContext.CustomerSales
                    .Include(s => s.Customer)
                    .Include(s => s.Branch)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductSerial)
                    .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken);
                if (sale == null)
                    return Error(404, "Sale not found.");
                if (sale.Status == SaleStatus.Voided)
                    return Error(400, $"Sale {sale.InvoiceNumber} is voided, so nothing can be returned from it.");

                var returnDate = request.ReturnDate ?? DateTime.UtcNow;
                if (returnDate < sale.SaleDate)
                    return Error(400, "The return date cannot be before the sale date.");

                var eligibility = await SaleReturnEligibility.ForSaleAsync(_dbContext, sale, cancellationToken);
                var picked = new List<(SaleDetails Line, int Quantity, bool Restock)>();
                foreach (var item in request.Lines)
                {
                    var line = sale.SaleDetails.FirstOrDefault(d => d.Id == item.SaleDetailsId);
                    if (line == null)
                        return Error(400, $"Line {item.SaleDetailsId} is not on sale {sale.InvoiceNumber}.");
                    var rule = eligibility[line.Id];
                    var name = line.ProductVariant.Product.ProductName;
                    if (rule.BlockedReason != null)
                        return Error(400, $"'{name}' cannot be returned: {rule.BlockedReason}");
                    if (item.Quantity > rule.Remaining)
                        return Error(400, $"'{name}': only {rule.Remaining} can still be returned (sold {rule.Sold}, already returned {rule.Returned}).");
                    picked.Add((line, item.Quantity, item.Restock));
                }

                // ---- What it is worth: the lines' share of the sale total.
                var grosses = picked.Select(p => p.Line.UnitPrice * p.Quantity).ToList();
                var thisGross = grosses.Sum();
                var returnedGross = await SaleReturnEligibility.ReturnedGrossAsync(_dbContext, sale.Id, cancellationToken);
                var everythingBack = eligibility.Values.All(e =>
                    e.Remaining - picked.Where(p => p.Line.Id == e.SaleDetailsId).Sum(p => p.Quantity) == 0);
                var total = ReturnMath.ReturnTotal(thisGross, returnedGross, sale.SubTotal, sale.TotalAmount,
                    sale.TotalAmount - sale.ReturnedAmount, everythingBack);
                var tax = Math.Min(total, ReturnMath.Share(thisGross, sale.SubTotal, sale.TaxAmount));
                var lineAmounts = ReturnMath.Split(total, grosses);

                var year = BusinessClock.ToLocal(returnDate).Year;
                var salesReturn = new SalesReturn
                {
                    ReturnNumber = await DocumentNumbers.NextAsync(
                        _dbContext, _dbContext.SalesReturns.IgnoreQueryFilters().Select(r => r.ReturnNumber),
                        $"SR-{year}-", cancellationToken),
                    SaleId = sale.Id,
                    CustomerId = sale.CustomerId,
                    BranchId = sale.BranchId,
                    ReturnDate = returnDate,
                    Status = ReturnStatus.Completed,
                    SubTotal = thisGross,
                    TaxAmount = tax,
                    TotalAmount = total,
                    DiscountAmount = thisGross + tax - total,
                    Reason = request.Reason.Trim(),
                    Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim(),
                    CustomerSale = sale,
                    Customer = sale.Customer,
                    Branch = sale.Branch,
                };

                // ---- Stock and serials.
                var restockVariants = picked.Where(p => p.Restock).Select(p => p.Line.ProductVariantId).ToList();
                if (restockVariants.Count > 0)
                    await _dbContext.LockStocksAsync([sale.BranchId], restockVariants, cancellationToken);
                var stocks = new Dictionary<int, Stock>();

                for (var i = 0; i < picked.Count; i++)
                {
                    var (line, quantity, restock) = picked[i];
                    line.ReturnedQuantity += quantity;
                    salesReturn.Lines.Add(new SalesReturnLine
                    {
                        SaleDetailsId = line.Id,
                        ProductVariantId = line.ProductVariantId,
                        ProductSerialId = line.ProductSerialId,
                        Quantity = quantity,
                        UnitPrice = line.UnitPrice,
                        Amount = lineAmounts[i],
                        UnitCost = line.UnitCost,
                        Restock = restock,
                        SalesReturn = salesReturn,
                        SaleDetails = line,
                        ProductVariant = line.ProductVariant,
                        ProductSerial = line.ProductSerial,
                    });

                    if (restock)
                    {
                        var stock = await ReturnMath.GetOrCreateStockAsync(_dbContext, stocks, sale.Branch, line.ProductVariant, cancellationToken);
                        stock.CurrentStock += quantity;
                        await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                        {
                            BranchId = sale.BranchId,
                            ProductVariantId = line.ProductVariantId,
                            SupplierPurchaseDetailsId = line.ProductSerial?.SupplierPurchaseDetailsId,
                            TransactionType = InventoryTxnType.ReturnIn,
                            QuantityIn = quantity,
                            QuantityOut = 0,
                            BalanceAfter = stock.CurrentStock,
                            TransactionDate = returnDate,
                            Branch = sale.Branch,
                            ProductVariant = line.ProductVariant,
                        }, cancellationToken);
                    }

                    if (line.ProductSerial is { } serial)
                    {
                        // A restocked unit can be sold again; a damaged one is set aside.
                        serial.Status = restock ? SerialStatus.InStock : SerialStatus.Defective;
                        serial.SoldDate = null;
                        serial.BranchId = sale.BranchId;
                    }
                }
                await _dbContext.SalesReturns.AddAsync(salesReturn, cancellationToken);

                // ---- Money.
                await _dbContext.LockRowAsync<Customer>(sale.CustomerId, cancellationToken);
                var balance = await _dbContext.CustomerTransactions
                    .Where(t => t.CustomerId == sale.CustomerId)
                    .GetLatestBalanceAsync(cancellationToken);

                var dueReduced = Math.Min(total, sale.DueAmount);
                balance -= total;
                await _dbContext.CustomerTransactions.AddAsync(
                    CustomerTransaction.ForReturn(salesReturn, sale.Customer, total, returnDate, balance), cancellationToken);

                // The part that was already paid: settlement first (no cash moves), then cash.
                var paidPart = total - dueReduced;
                var (reversed, balanceAfterReversal) = await new CrossLedgerSettlement(_dbContext).ReverseOnSaleAsync(
                    sale, paidPart, balance, returnDate,
                    $"Settlement reversed: return {salesReturn.ReturnNumber}", cancellationToken);
                balance = balanceAfterReversal;

                var cash = paidPart - reversed;
                if (cash > 0)
                {
                    var refund = new CustomerPayment
                    {
                        CustomerId = sale.CustomerId,
                        BranchId = sale.BranchId,
                        Amount = -cash,
                        PaymentDate = returnDate,
                        PaymentMethod = "Refund",
                        Remarks = $"Refund for return {salesReturn.ReturnNumber} (sale {sale.InvoiceNumber})",
                        Customer = sale.Customer,
                        Branch = sale.Branch,
                    };
                    await _dbContext.CustomerPayments.AddAsync(refund, cancellationToken);
                    await _dbContext.SaleCustomerPayments.AddAsync(new SaleCustomerPayment
                    {
                        Amount = -cash,
                        AllocationDate = returnDate,
                        CustomerSale = sale,
                        CustomerPayment = refund,
                    }, cancellationToken);
                    sale.ReversePayment(cash);
                    balance += cash;
                    await _dbContext.CustomerTransactions.AddAsync(
                        CustomerTransaction.ForRefund(refund, sale.Customer, cash, balance, sale), cancellationToken);
                }

                // The settlement reversal and the refund each put their part back on the due
                // (ReversePayment); the whole return then comes off it. Net: due drops by dueReduced.
                sale.DueAmount -= total;
                sale.ReturnedAmount += total;
                sale.UpDatedAt = DateTime.UtcNow;

                salesReturn.DueReduced = dueReduced;
                salesReturn.SettlementReversed = reversed;
                salesReturn.CashRefunded = cash;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = $"Return {salesReturn.ReturnNumber} saved: {total:0.00}"
                              + (dueReduced > 0 ? $", {dueReduced:0.00} taken off the due" : "")
                              + (reversed > 0 ? $", {reversed:0.00} supplier settlement reversed" : "")
                              + (cash > 0 ? $", {cash:0.00} to refund in cash" : "")
                              + ".",
                    Data = new
                    {
                        salesReturn.Id,
                        salesReturn.ReturnNumber,
                        salesReturn.TotalAmount,
                        salesReturn.DueReduced,
                        salesReturn.SettlementReversed,
                        salesReturn.CashRefunded,
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating sales return for sale {SaleId}", request.SaleId);
                return Error(500, "An error occurred while saving the return.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
