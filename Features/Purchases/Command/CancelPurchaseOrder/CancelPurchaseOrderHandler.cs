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
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Purchases.Command.CancelPurchaseOrder
{
    // Cancels an approved purchase ("Delete Approved Purchase"). One transaction; nothing is deleted
    // except serial rows of units that never left the shelf (their numbers become reusable).
    //
    //   Load     lock document -> stock -> linked customer -> supplier, re-check the status under the lock
    //   Check    BEFORE anything changes: every unit still attributable to the bill must still be on
    //            the shelf (never sold, moved, claimed or returned) - otherwise refuse and point at
    //            Purchase Return. Sold / consumed stock is never silently deducted
    //   Stock    only what remains: received - already returned to the supplier. Returned units
    //            already left with their own return, so they are not deducted twice
    //   Credit   only a Credit bill: its exact cross-ledger settlements are reversed (the customer
    //            sales they paid are due again). A Cash bill never touches settlement
    //   Ledger   a "PurchaseCancel" row takes total - returned off what we owe
    //   Money    payments are never deleted: their share on this bill is released with negative
    //            allocations and stays our advance with the supplier. A supplier refund exists only if asked
    //   Record   bill -> Cancelled (lines closed), one PurchaseEdits audit row with every reference
    public class CancelPurchaseOrderHandler(
        AppDbContext _dbContext,
        ILogger<CancelPurchaseOrderHandler> _logger
    ) : IRequestHandler<CancelPurchaseOrderCommand, Result>
    {
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

        public async Task<Result> Handle(CancelPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // ---------------------------------------------------------------- lock + load
                await _dbContext.LockRowAsync<SupplierPurchase>(request.Id, cancellationToken);
                var head = await _dbContext.SupplierPurchases
                    .AsNoTracking()
                    .Where(p => p.Id == request.Id && p.IsActive != (int)EntityStatus.Deleted)
                    .Select(p => new { p.BranchId, p.SupplierId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (head == null)
                    return Error(404, "Purchase order not found.");

                // Stock before the rows are read, so serial statuses and quantities are current.
                var variantIds = await _dbContext.SupplierPurchaseDetails.IgnoreQueryFilters()
                    .Where(d => d.PurchaseId == request.Id)
                    .Select(d => d.ProductVariantId).Distinct().ToListAsync(cancellationToken);
                if (variantIds.Count > 0)
                    await _dbContext.LockStocksAsync([head.BranchId], variantIds, cancellationToken);

                var purchase = await _dbContext.SupplierPurchases
                    .Include(p => p.Supplier)
                    .Include(p => p.Branch)
                    .Include(p => p.SupplierPurchaseDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .Include(p => p.SupplierPurchaseDetails).ThenInclude(d => d.ProductSerials)
                    .FirstAsync(p => p.Id == request.Id, cancellationToken);

                switch (purchase.Status)
                {
                    case PurchaseStatus.Cancelled:
                        return Error(400, $"Purchase {purchase.InvoiceNumber} is already cancelled.");
                    case PurchaseStatus.Pending:
                    case PurchaseStatus.Rejected:
                        return Error(400, $"Purchase {purchase.InvoiceNumber} is {purchase.Status}: nothing was received, so use the normal Delete for it.");
                    case PurchaseStatus.PartiallyReceived:
                        return Error(400, $"Purchase {purchase.InvoiceNumber} is only partially received. Receive the rest (or return what came) first; only an approved purchase can be cancelled.");
                }
                if (purchase.PurchaseType == null)
                    return Error(400, "This purchase has no payment type, so it cannot be cancelled.");

                var lines = purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected).ToList();
                var returnedByLine = await _dbContext.PurchaseReturnLines.IgnoreQueryFilters()
                    .Where(l => l.PurchaseReturn.PurchaseId == purchase.Id && l.PurchaseReturn.Status == ReturnStatus.Completed)
                    .GroupBy(l => l.SupplierPurchaseDetailsId)
                    .Select(g => new { g.Key, Quantity = g.Sum(l => l.Quantity) })
                    .ToDictionaryAsync(x => x.Key, x => x.Quantity, cancellationToken);

                var reversal = new TransactionReversal(_dbContext);

                // ---------------------------------------------------------------- checks
                var allSerialIds = lines.SelectMany(d => d.ProductSerials).Select(s => s.Id).ToList();
                var inUse = await reversal.SerialsWithHistoryAsync(allSerialIds, cancellationToken);

                var plan = new List<(SupplierPurchaseDetails Line, int Units, List<ProductSerial> Serials)>();
                foreach (var line in lines)
                {
                    var name = line.ProductVariant.Product.ProductName;
                    var received = line.ReceivedQuantity ?? 0;
                    var remaining = Math.Max(0, received - returnedByLine.GetValueOrDefault(line.Id));

                    if (line.ProductVariant.IsSerialized)
                    {
                        // Units already sent back (RmaReturned) left with their return.
                        var onHand = line.ProductSerials
                            .Where(s => s.Status != SerialStatus.Ordered && s.Status != SerialStatus.RmaReturned).ToList();
                        foreach (var s in onHand)
                        {
                            if (s.Status != SerialStatus.InStock)
                                return Error(400, $"'{name}': serial {s.SerialNumber} is {s.Status}, so the purchase cannot be cancelled. Use Purchase Return for goods that have to go back.");
                            if (s.BranchId != purchase.BranchId || s.StockTransferDetailsId != null)
                                return Error(400, $"'{name}': serial {s.SerialNumber} was moved out of this warehouse, so the purchase cannot be cancelled.");
                            if (inUse.Contains(s.Id))
                                return Error(400, $"'{name}': serial {s.SerialNumber} has a sale, return or warranty history, so the purchase cannot be cancelled.");
                        }
                        plan.Add((line, onHand.Count, onHand));
                    }
                    else if (remaining > 0)
                    {
                        plan.Add((line, remaining, []));
                    }
                }

                var stocks = new Dictionary<int, Stock>();
                foreach (var group in plan.Where(p => p.Units > 0).GroupBy(p => p.Line.ProductVariantId))
                {
                    var variant = group.First().Line.ProductVariant;
                    var units = group.Sum(p => p.Units);
                    var stock = await GetStockAsync(stocks, purchase.Branch, variant, cancellationToken);
                    if (stock.CurrentStock < units)
                        return Error(400, $"Cannot take {units} unit(s) of '{variant.Product.ProductName}' off this bill: only {stock.CurrentStock} on hand, the rest was sold or moved. Use Purchase Return for goods that have to go back.");
                }

                // ---------------------------------------------------------------- locks: customer -> supplier
                var supplier = purchase.Supplier;
                var linkedCustomerId = await _dbContext.Customers
                    .Where(c => c.SupplierId == purchase.SupplierId)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (linkedCustomerId is int customerId)
                    await _dbContext.LockRowAsync<Customer>(customerId, cancellationToken);
                await _dbContext.LockRowAsync<Supplier>(supplier.Id, cancellationToken);

                var now = DateTime.UtcNow;
                var old = new { purchase.DiscountAmount, purchase.TotalAmount, purchase.PaidAmount, purchase.DueAmount };
                var returned = purchase.ReturnedAmount;
                var beforeLines = lines.Select(l => new
                {
                    id = l.Id,
                    productName = l.ProductVariant.Product.ProductName,
                    sku = l.ProductVariant.SKU,
                    received = l.ReceivedQuantity ?? 0,
                    returnedQuantity = returnedByLine.GetValueOrDefault(l.Id),
                    unitPrice = l.UnitPrice,
                    totalAmount = l.TotalAmount,
                    serials = l.ProductSerials.Where(s => s.Status != SerialStatus.Ordered).Select(s => new { s.SerialNumber, status = s.Status.ToString() }).ToList(),
                }).ToList();
                var earlierReturns = await _dbContext.PurchaseReturns.IgnoreQueryFilters()
                    .Where(r => r.PurchaseId == purchase.Id)
                    .Select(r => new { r.Id, r.ReturnNumber, status = r.Status.ToString(), r.TotalAmount })
                    .ToListAsync(cancellationToken);

                // ---------------------------------------------------------------- stock
                var stockMoves = new List<object>();
                foreach (var (line, units, serials) in plan.Where(p => p.Units > 0))
                {
                    var balanceAfter = await reversal.MoveStockAsync(
                        stocks, purchase.Branch, line.ProductVariant, -units, line, now, cancellationToken);
                    stockMoves.Add(new
                    {
                        variantId = line.ProductVariantId,
                        line.ProductVariant.SKU,
                        productName = line.ProductVariant.Product.ProductName,
                        change = -units,
                        balanceAfter,
                        removedSerials = serials.Select(s => s.SerialNumber).ToList(),
                    });
                    // The units never left the shelf and have no history: their numbers become reusable.
                    _dbContext.ProductSerials.RemoveRange(serials);
                }

                // ---------------------------------------------------------------- settlement (Credit only)
                var balance = await _dbContext.SupplierTransactions
                    .Where(t => t.SupplierId == supplier.Id)
                    .GetLatestBalanceAsync(cancellationToken);

                decimal settlementReversed = 0;
                if (purchase.PurchaseType == PurchaseType.Debit)
                {
                    var settled = await _dbContext.SupplierPurchasePayments.IgnoreQueryFilters()
                        .Where(a => a.SupplierPurchaseId == purchase.Id && a.SupplierPayment.PaymentMethod == PartySettlement.PaymentMethod)
                        .SumAsync(a => (decimal?)a.Amount, cancellationToken) ?? 0m;
                    if (settled > 0)
                    {
                        var undone = await new CrossLedgerSettlement(_dbContext).ReverseOnPurchaseAsync(
                            purchase, settled, balance, now, $"Settlement undone: bill {purchase.InvoiceNumber} cancelled", cancellationToken);
                        settlementReversed = undone.Reversed;
                        balance = undone.SupplierBalance;
                    }
                }

                // ---------------------------------------------------------------- ledger
                var owed = purchase.TotalAmount - returned;
                if (owed > 0)
                {
                    balance -= owed;
                    await _dbContext.SupplierTransactions.AddAsync(
                        SupplierTransaction.ForPurchaseCancel(purchase, supplier, owed, now, balance), cancellationToken);
                }

                // ---------------------------------------------------------------- payments
                var cash = purchase.PaidAmount;
                decimal cashRefunded = 0;
                if (request.RefundCash && cash > 0)
                {
                    // Only on the user's say-so: the supplier really paid it back. The negative
                    // "Refund" payment undoes the allocation on the bill, so nothing stays as an advance.
                    balance = await reversal.RefundFromSupplierAsync(purchase, supplier, cash, balance, now, cancellationToken);
                    cashRefunded = cash;
                }
                else
                {
                    var released = await reversal.ReleasePurchaseCashAsync(purchase, cash, now, cancellationToken);
                    if (released != cash)
                        return Error(500, "The bill's payments could not be released cleanly; nothing was changed.");
                }

                // ---------------------------------------------------------------- the record
                // Cost averages count only lines that received something, so a cancelled line receives nothing.
                foreach (var line in lines)
                {
                    line.ReceivedQuantity = 0;
                    line.Status = LineStatus.Rejected;
                }
                purchase.PaidAmount = 0;
                purchase.DueAmount = 0;
                purchase.Status = PurchaseStatus.Cancelled;
                purchase.UpDatedAt = now;

                var added = _dbContext.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added).Select(e => e.Entity).ToList();
                await _dbContext.SaveChangesAsync(cancellationToken);

                var advance = cash - cashRefunded;
                await _dbContext.PurchaseEdits.AddAsync(new PurchaseEdit
                {
                    PurchaseId = purchase.Id,
                    SupplierPurchase = purchase,
                    Reason = request.Reason.Trim(),
                    OldDiscountAmount = old.DiscountAmount, OldTotalAmount = old.TotalAmount,
                    OldPaidAmount = old.PaidAmount, OldDueAmount = old.DueAmount,
                    NewDiscountAmount = old.DiscountAmount, NewTotalAmount = old.TotalAmount,
                    NewPaidAmount = 0, NewDueAmount = 0,
                    ReturnedAmount = returned,
                    SettlementReversed = settlementReversed,
                    CashReleased = cash,
                    CashRefunded = cashRefunded,
                    ChangesJson = JsonSerializer.Serialize(new
                    {
                        kind = "Cancellation",
                        documentType = "Purchase",
                        invoiceNumber = purchase.InvoiceNumber,
                        purchaseType = purchase.PurchaseType.ToString(),
                        originalTotal = old.TotalAmount,
                        payableReversed = owed > 0 ? owed : 0m,
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
                    Message = $"Purchase {purchase.InvoiceNumber} cancelled."
                              + (stockMoves.Count > 0 ? " Stock taken back out." : "")
                              + (settlementReversed > 0 ? $" {settlementReversed:0.00} customer settlement reversed." : "")
                              + (advance > 0 ? $" {advance:0.00} paid to the supplier stays as our advance with them." : "")
                              + (cashRefunded > 0 ? $" {cashRefunded:0.00} recorded as refunded by the supplier in cash." : ""),
                    Data = new
                    {
                        purchase.Id,
                        purchase.InvoiceNumber,
                        Status = purchase.Status.ToString(),
                        PurchaseType = purchase.PurchaseType.ToString(),
                        OriginalTotal = old.TotalAmount,
                        Returned = returned,
                        PayableReversed = owed > 0 ? owed : 0m,
                        SettlementReversed = settlementReversed,
                        Advance = advance,
                        CashRefunded = cashRefunded,
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling purchase {PurchaseId}", request.Id);
                return Error(500, "An error occurred while cancelling the purchase. Nothing was changed.");
            }
        }

        private Task<Stock> GetStockAsync(
            Dictionary<int, Stock> stocks, Branch branch, ProductVariant variant, CancellationToken cancellationToken) =>
            Features.Returns.Shared.ReturnMath.GetOrCreateStockAsync(_dbContext, stocks, branch, variant, cancellationToken);

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
