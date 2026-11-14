using System.Text.Json;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Parties.Shared;
using Inventory_Management_System.Features.Reversals.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Command.CancelSale
{
    // Cancels a completed sale ("Delete Invoice"). One transaction; nothing is deleted.
    //
    //   Load     lock document -> stock -> customer (-> supplier inside the settlement reversal),
    //            re-check the status under the lock (a second click / a racing return waits here)
    //   Check    warranty claims, serials still sold - refuse BEFORE changing anything
    //   Stock    only what is still out: quantity - already returned. Returned units were put back
    //            by their own return, so they are not restored twice. One Adjustment row per variant
    //   Credit   only a Credit sale: its exact cross-ledger settlements are reversed (the supplier
    //            bills they paid are due again). A Cash sale never touches settlement
    //   Ledger   a "Void" row takes total - returned off the receivable (returns keep their rows)
    //   Money    payments are never deleted: their share on this invoice is released with negative
    //            allocations and stays the customer's advance. A cash refund exists only if asked
    //   Record   sale + lines -> Voided (shown as Cancelled), one SaleEdits audit row with every
    //            reference the cancellation wrote
    public class CancelSaleHandler(
        AppDbContext _dbContext,
        ILogger<CancelSaleHandler> _logger
    ) : IRequestHandler<CancelSaleCommand, Result>
    {
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

        public async Task<Result> Handle(CancelSaleCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // ---------------------------------------------------------------- load + lock
                await _dbContext.LockRowAsync<CustomerSale>(request.Id, cancellationToken);
                var sale = await _dbContext.CustomerSales
                    .Include(s => s.Customer)
                    .Include(s => s.Branch)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .Include(s => s.SaleDetails).ThenInclude(d => d.ProductSerial)
                    .FirstOrDefaultAsync(s => s.Id == request.Id && s.IsActive != (int)EntityConstant.EntityStatus.Deleted, cancellationToken);
                if (sale == null)
                    return Error(404, "Sale not found.");
                if (sale.Status == SaleStatus.Voided)
                    return Error(400, $"Invoice {sale.InvoiceNumber} is already cancelled.");

                var lines = sale.SaleDetails.Where(d => d.Status == SaleLineStatus.Completed).ToList();

                // ---------------------------------------------------------------- checks
                var stillOut = lines.Where(l => l.Quantity - l.ReturnedQuantity > 0).ToList();
                foreach (var line in stillOut)
                {
                    var name = line.ProductVariant.Product.ProductName;
                    if (await _dbContext.WarrantyClaims.IgnoreQueryFilters().AnyAsync(c => c.SaleDetailsId == line.Id, cancellationToken))
                        return Error(400, $"'{name}' has a warranty claim, so the invoice cannot be cancelled.");
                    if (line.ProductVariant.IsSerialized && line.ProductSerial is { } serial && serial.Status != SerialStatus.Sold)
                        return Error(400, $"Serial {serial.SerialNumber} of '{name}' is now {serial.Status}, so the invoice cannot be cancelled.");
                }

                var variants = stillOut.Select(l => l.ProductVariantId).Distinct().ToList();
                if (variants.Count > 0)
                    await _dbContext.LockStocksAsync([sale.BranchId], variants, cancellationToken);

                var customer = sale.Customer;
                await _dbContext.LockRowAsync<Customer>(customer.Id, cancellationToken);
                var now = DateTime.UtcNow;

                var old = new { sale.SubTotal, sale.DiscountAmount, sale.TaxAmount, sale.TotalAmount, sale.PaidAmount, sale.DueAmount };
                var returned = sale.ReturnedAmount;
                var beforeLines = lines.Select(l => new
                {
                    id = l.Id,
                    productName = l.ProductVariant.Product.ProductName,
                    sku = l.ProductVariant.SKU,
                    quantity = l.Quantity,
                    returnedQuantity = l.ReturnedQuantity,
                    unitPrice = l.UnitPrice,
                    totalAmount = l.TotalAmount,
                    serialNumber = l.ProductSerial?.SerialNumber,
                }).ToList();
                var earlierReturns = await _dbContext.SalesReturns.IgnoreQueryFilters()
                    .Where(r => r.SaleId == sale.Id)
                    .Select(r => new { r.Id, r.ReturnNumber, status = r.Status.ToString(), r.TotalAmount })
                    .ToListAsync(cancellationToken);

                var reversal = new TransactionReversal(_dbContext);

                // ---------------------------------------------------------------- stock
                var stocks = new Dictionary<int, Stock>();
                var stockMoves = new List<object>();
                foreach (var group in stillOut.GroupBy(l => l.ProductVariantId))
                {
                    var variant = group.First().ProductVariant;
                    var units = group.Sum(l => l.Quantity - l.ReturnedQuantity);
                    var balanceAfter = await reversal.MoveStockAsync(stocks, sale.Branch, variant, units, null, now, cancellationToken);
                    stockMoves.Add(new { variantId = variant.Id, variant.SKU, productName = variant.Product.ProductName, change = units, balanceAfter });
                }
                foreach (var line in stillOut.Where(l => l.ProductSerial != null))
                {
                    line.ProductSerial!.Status = SerialStatus.InStock;
                    line.ProductSerial.SoldDate = null;
                }

                // ---------------------------------------------------------------- settlement (Credit only)
                var balance = await _dbContext.CustomerTransactions
                    .Where(t => t.CustomerId == customer.Id)
                    .GetLatestBalanceAsync(cancellationToken);

                decimal settlementReversed = 0;
                if (sale.SaleType == SaleType.Debit)
                {
                    var settled = await _dbContext.SaleCustomerPayments.IgnoreQueryFilters()
                        .Where(a => a.SaleId == sale.Id && a.CustomerPayment.PaymentMethod == PartySettlement.PaymentMethod)
                        .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;
                    if (settled > 0)
                    {
                        var undone = await new CrossLedgerSettlement(_dbContext).ReverseOnSaleAsync(
                            sale, settled, balance, now, $"Settlement undone: invoice {sale.InvoiceNumber} cancelled", cancellationToken);
                        settlementReversed = undone.Reversed;
                        balance = undone.CustomerBalance;
                    }
                }

                // ---------------------------------------------------------------- ledger
                var owed = sale.TotalAmount - returned;
                if (owed > 0)
                {
                    balance -= owed;
                    await _dbContext.CustomerTransactions.AddAsync(
                        CustomerTransaction.ForCancel(sale, customer, owed, now, balance), cancellationToken);
                }

                // ---------------------------------------------------------------- payments
                // What is still paid on the invoice after the settlement reversal is real money.
                var cash = sale.PaidAmount;
                decimal cashRefunded = 0;
                if (request.RefundCash && cash > 0)
                {
                    // Only on the user's say-so: the money was really handed back. The negative
                    // "Refund" payment undoes the allocation on the invoice, so the customer's
                    // account nets to zero and nothing stays as an advance.
                    balance = await reversal.RefundCustomerAsync(sale, customer, cash, balance, now, cancellationToken);
                    cashRefunded = cash;
                }
                else
                {
                    var released = await reversal.ReleaseSaleCashAsync(sale, cash, now, cancellationToken);
                    if (released != cash)
                        return Error(500, "The invoice's payments could not be released cleanly; nothing was changed.");
                }

                // ---------------------------------------------------------------- the record
                foreach (var line in lines)
                    line.Status = SaleLineStatus.Voided;
                sale.PaidAmount = 0;
                sale.DueAmount = 0;
                sale.Status = SaleStatus.Voided;
                sale.VoidedAt = now;
                sale.VoidReason = request.Reason.Trim();
                sale.UpDatedAt = now;

                // Flush so every new row has its id, then write the audit row that points at them.
                var added = _dbContext.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added).Select(e => e.Entity).ToList();
                await _dbContext.SaveChangesAsync(cancellationToken);

                var advance = cash - cashRefunded;
                await _dbContext.SaleEdits.AddAsync(new SaleEdit
                {
                    SaleId = sale.Id,
                    CustomerSale = sale,
                    Reason = request.Reason.Trim(),
                    OldSubTotal = old.SubTotal, OldDiscountAmount = old.DiscountAmount, OldTaxAmount = old.TaxAmount,
                    OldTotalAmount = old.TotalAmount, OldPaidAmount = old.PaidAmount, OldDueAmount = old.DueAmount,
                    NewSubTotal = old.SubTotal, NewDiscountAmount = old.DiscountAmount, NewTaxAmount = old.TaxAmount,
                    NewTotalAmount = old.TotalAmount, NewPaidAmount = 0, NewDueAmount = 0,
                    ReturnedAmount = returned,
                    SettlementReversed = settlementReversed,
                    CashReleased = cash,
                    CashRefunded = cashRefunded,
                    ChangesJson = JsonSerializer.Serialize(new
                    {
                        kind = "Cancellation",
                        documentType = "Sale",
                        invoiceNumber = sale.InvoiceNumber,
                        saleType = sale.SaleType.ToString(),
                        originalTotal = old.TotalAmount,
                        receivableReversed = owed > 0 ? owed : 0m,
                        advanceKept = advance,
                        before = beforeLines,
                        after = Array.Empty<object>(),
                        returns = earlierReturns,
                        stock = stockMoves,
                        references = TransactionReversal.DescribeAdded(added),
                    }, Json),
                }, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Invoice {sale.InvoiceNumber} cancelled."
                              + (stockMoves.Count > 0 ? " Stock restored." : "")
                              + (settlementReversed > 0 ? $" {settlementReversed:0.00} supplier settlement reversed." : "")
                              + (advance > 0 ? $" {advance:0.00} paid by the customer stays as their advance." : "")
                              + (cashRefunded > 0 ? $" {cashRefunded:0.00} recorded as refunded in cash." : ""),
                    Data = new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        Status = sale.Status.ToString(),
                        SaleType = sale.SaleType.ToString(),
                        OriginalTotal = old.TotalAmount,
                        Returned = returned,
                        ReceivableReversed = owed > 0 ? owed : 0m,
                        SettlementReversed = settlementReversed,
                        Advance = advance,
                        CashRefunded = cashRefunded,
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling sale {SaleId}", request.Id);
                return Error(500, "An error occurred while cancelling the invoice. Nothing was changed.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
