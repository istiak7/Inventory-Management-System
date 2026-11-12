using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.SalesReturns.Command.CancelSalesReturn
{
    // Undoes a sales return entered by mistake. The goods go back to the customer (restocked
    // units leave stock again; damaged serials go back to Sold) and the return's whole amount
    // is owed on the sale again. The cash refund and settlement reversal it made stay as
    // their own history: collect that money with a normal payment.
    // Refused when a restocked unit was already sold again or moved.
    public class CancelSalesReturnHandler(
        AppDbContext _dbContext,
        ILogger<CancelSalesReturnHandler> _logger
    ) : IRequestHandler<CancelSalesReturnCommand, Result>
    {
        public async Task<Result> Handle(CancelSalesReturnCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<SalesReturn>(request.Id, cancellationToken);
                var salesReturn = await _dbContext.SalesReturns
                    .Include(r => r.CustomerSale)
                    .Include(r => r.Customer)
                    .Include(r => r.Branch)
                    .Include(r => r.Lines).ThenInclude(l => l.ProductVariant).ThenInclude(v => v.Product)
                    .Include(r => r.Lines).ThenInclude(l => l.ProductSerial)
                    .Include(r => r.Lines).ThenInclude(l => l.SaleDetails)
                    .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
                if (salesReturn == null)
                    return Error(404, "Return not found.");
                if (salesReturn.Status == ReturnStatus.Cancelled)
                    return Error(400, $"Return {salesReturn.ReturnNumber} is already cancelled.");

                var sale = salesReturn.CustomerSale;
                var restocked = salesReturn.Lines.Where(l => l.Restock).ToList();
                if (restocked.Count > 0)
                    await _dbContext.LockStocksAsync([salesReturn.BranchId], restocked.Select(l => l.ProductVariantId), cancellationToken);

                // ---- Check everything first, so nothing is half-undone.
                var stocks = new Dictionary<int, Stock>();
                foreach (var group in restocked.GroupBy(l => l.ProductVariantId))
                {
                    var variant = group.First().ProductVariant;
                    var stock = await ReturnMath.GetOrCreateStockAsync(_dbContext, stocks, salesReturn.Branch, variant, cancellationToken);
                    var needed = group.Sum(l => l.Quantity);
                    if (stock.CurrentStock < needed)
                        return Error(400, $"'{variant.Product.ProductName}': only {stock.CurrentStock} in stock, so the {needed} returned cannot be taken back out. It was probably sold or moved.");
                }
                foreach (var line in salesReturn.Lines.Where(l => l.ProductSerial != null))
                {
                    var serial = line.ProductSerial!;
                    var expected = line.Restock ? SerialStatus.InStock : SerialStatus.Defective;
                    if (serial.Status != expected || serial.BranchId != salesReturn.BranchId)
                        return Error(400, $"Serial {serial.SerialNumber} is now {serial.Status}, so this return cannot be cancelled.");
                }

                var now = DateTime.UtcNow;

                // ---- Goods back to the customer.
                foreach (var line in salesReturn.Lines)
                    line.SaleDetails.ReturnedQuantity -= line.Quantity;
                foreach (var line in restocked)
                {
                    var stock = stocks[line.ProductVariantId];
                    stock.CurrentStock -= line.Quantity;
                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = salesReturn.BranchId,
                        ProductVariantId = line.ProductVariantId,
                        SupplierPurchaseDetailsId = line.ProductSerial?.SupplierPurchaseDetailsId,
                        TransactionType = InventoryTxnType.SaleOut,
                        QuantityIn = 0,
                        QuantityOut = line.Quantity,
                        BalanceAfter = stock.CurrentStock,
                        TransactionDate = now,
                        Branch = salesReturn.Branch,
                        ProductVariant = line.ProductVariant,
                    }, cancellationToken);
                }
                foreach (var line in salesReturn.Lines.Where(l => l.ProductSerial != null))
                {
                    line.ProductSerial!.Status = SerialStatus.Sold;
                    line.ProductSerial.SoldDate = sale.SaleDate;
                }

                // ---- Money: the return's amount is owed on the sale again.
                await _dbContext.LockRowAsync<Customer>(salesReturn.CustomerId, cancellationToken);
                var balance = await _dbContext.CustomerTransactions
                    .Where(t => t.CustomerId == salesReturn.CustomerId)
                    .GetLatestBalanceAsync(cancellationToken);
                balance += salesReturn.TotalAmount;
                await _dbContext.CustomerTransactions.AddAsync(
                    CustomerTransaction.ForReturn(salesReturn, salesReturn.Customer, -salesReturn.TotalAmount, now, balance),
                    cancellationToken);

                sale.ReturnedAmount -= salesReturn.TotalAmount;
                sale.DueAmount += salesReturn.TotalAmount;
                sale.UpDatedAt = now;

                salesReturn.Status = ReturnStatus.Cancelled;
                salesReturn.CancelledAt = now;
                salesReturn.CancelReason = request.Reason.Trim();
                salesReturn.UpDatedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var handedBack = salesReturn.CashRefunded + salesReturn.SettlementReversed;
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Return {salesReturn.ReturnNumber} cancelled. {salesReturn.TotalAmount:0.00} is due on sale {sale.InvoiceNumber} again."
                              + (handedBack > 0 ? $" ({handedBack:0.00} of it had been refunded or settled — collect it with a payment.)" : ""),
                    Data = new { salesReturn.Id, salesReturn.ReturnNumber, Status = salesReturn.Status.ToString() }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling sales return {Id}", request.Id);
                return Error(500, "An error occurred while cancelling the return.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
