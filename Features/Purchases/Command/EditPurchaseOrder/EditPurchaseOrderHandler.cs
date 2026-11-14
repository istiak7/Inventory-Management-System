using System.Text.Json;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Parties.Shared;
using Inventory_Management_System.Features.Purchases.Shared;
using Inventory_Management_System.Features.Returns.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Purchases.Command.EditPurchaseOrder
{
    // Edits an APPROVED purchase without rewriting history. Everything runs in ONE transaction.
    //
    //   Phase A  load the bill, its lines, serials, payments; lock document -> stock -> customer
    //            (the linked party, if any) -> supplier
    //   Phase B  undo: a Credit bill's cross-ledger settlements are reversed (reversal rows in
    //            both ledgers); actual payments to the supplier are never touched
    //   Phase C  apply the new lines, costs and discount. Stock moves only by the difference, as
    //            "Adjustment" inventory rows. A dropped line is closed (Rejected, nothing received)
    //            and stays on the record; earlier purchase returns are left exactly as they are
    //   Phase D  redo: due = new total - paid - returned. Overpaid -> the surplus payment is
    //            released from the bill and stays our advance with the supplier (or is booked as
    //            refunded in cash if asked); money released earlier is put back first; a Cash bill
    //            must be settled by a real payment; a Credit bill then runs cross-ledger FIFO again
    //   Audit    one PurchaseEdits row next to the ledger, payment, settlement and inventory rows
    //
    // Units can only come off the bill while they are still on the shelf, and a serial can only be
    // removed while it is in stock with no history: sold, moved, returned or claimed units are
    // never silently taken back (use a Purchase Return for goods that have to go back).
    public class EditPurchaseOrderHandler(
        AppDbContext _dbContext,
        ILogger<EditPurchaseOrderHandler> _logger
    ) : IRequestHandler<EditPurchaseOrderCommand, Result>
    {
        private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

        // A stock change on one line: units added (+) or taken off (-) the bill.
        private sealed record StockMove(SupplierPurchaseDetails Detail, ProductVariant Variant, int Delta);

        public async Task<Result> Handle(EditPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // ---------------------------------------------------------------- Phase A
                await _dbContext.LockRowAsync<SupplierPurchase>(request.Id, cancellationToken);
                var purchase = await _dbContext.SupplierPurchases
                    .Include(p => p.Supplier)
                    .Include(p => p.Branch)
                    .Include(p => p.SupplierPurchaseDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                    .Include(p => p.SupplierPurchaseDetails).ThenInclude(d => d.ProductSerials)
                    .FirstOrDefaultAsync(p => p.Id == request.Id && p.IsActive != (int)EntityStatus.Deleted, cancellationToken);
                if (purchase == null)
                    return Error(404, "Purchase order not found.");
                if (purchase.Status != PurchaseStatus.Approved)
                    return Error(400, $"Purchase {purchase.InvoiceNumber} is {purchase.Status}. Only an approved purchase is edited here; a pending order is edited from its normal Edit.");
                if (purchase.PurchaseType == null)
                    return Error(400, "This purchase has no payment type, so it cannot be edited.");

                var existing = purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected).ToDictionary(d => d.Id);

                var named = new HashSet<int>();
                foreach (var item in request.Items.Where(i => i.PurchaseDetailsId.HasValue))
                {
                    var lineId = item.PurchaseDetailsId!.Value;
                    if (!existing.ContainsKey(lineId))
                        return Error(400, $"Line {lineId} is not on purchase {purchase.InvoiceNumber}.");
                    if (!named.Add(lineId))
                        return Error(400, $"Line {lineId} is listed more than once.");
                }
                var removed = existing.Values.Where(d => !named.Contains(d.Id)).ToList();

                var variantIds = existing.Values.Select(d => d.ProductVariantId)
                    .Concat(request.Items.Select(i => i.ProductVariantId)).Distinct().ToList();
                await _dbContext.LockStocksAsync([purchase.BranchId], variantIds, cancellationToken);

                var returnedByLine = await _dbContext.PurchaseReturnLines.IgnoreQueryFilters()
                    .Where(l => l.PurchaseReturn.PurchaseId == purchase.Id && l.PurchaseReturn.Status == ReturnStatus.Completed)
                    .GroupBy(l => l.SupplierPurchaseDetailsId)
                    .Select(g => new { g.Key, Quantity = g.Sum(l => l.Quantity) })
                    .ToDictionaryAsync(x => x.Key, x => x.Quantity, cancellationToken);

                var beforeLines = existing.Values.Select(Snapshot).ToList();
                var old = new { purchase.DiscountAmount, purchase.TotalAmount, purchase.PaidAmount, purchase.DueAmount };
                var now = DateTime.UtcNow;

                // Which serials may be removed: in stock at the bill's branch, never moved, and
                // untouched by any sale, return or warranty claim.
                var allSerialIds = existing.Values.SelectMany(d => d.ProductSerials).Select(s => s.Id).ToList();
                var inUse = await SerialsWithHistoryAsync(allSerialIds, cancellationToken);
                bool Removable(ProductSerial s) =>
                    s.Status == SerialStatus.InStock && s.BranchId == purchase.BranchId
                    && s.StockTransferDetailsId == null && !inUse.Contains(s.Id);

                // ---------------------------------------------------------------- Phase C: lines
                var moves = new List<StockMove>();
                var newSerialNumbers = new List<string>();

                foreach (var line in removed)
                {
                    var name = line.ProductVariant.Product.ProductName;
                    if (returnedByLine.GetValueOrDefault(line.Id) > 0)
                        return Error(400, $"'{name}' has units returned to the supplier, so its line cannot be removed. You can reduce it down to the returned quantity.");

                    var received = line.ReceivedQuantity ?? 0;
                    if (line.ProductVariant.IsSerialized)
                    {
                        var serials = line.ProductSerials.Where(s => s.Status != SerialStatus.Ordered).ToList();
                        var stuck = serials.FirstOrDefault(s => !Removable(s));
                        if (stuck != null)
                            return Error(400, $"'{name}': serial {stuck.SerialNumber} is {SerialReason(stuck, inUse)}, so this line cannot be removed.");
                        _dbContext.ProductSerials.RemoveRange(serials);
                        moves.Add(new StockMove(line, line.ProductVariant, -serials.Count));
                    }
                    else if (received > 0)
                    {
                        moves.Add(new StockMove(line, line.ProductVariant, -received));
                    }

                    // The line stays on the record but costs nothing and brings nothing in
                    // (cost averages and reports only count lines that received something).
                    line.ReceivedQuantity = 0;
                    line.TotalAmount = 0;
                    line.Status = LineStatus.Rejected;
                }

                foreach (var item in request.Items)
                {
                    var error = item.PurchaseDetailsId is int lineId
                        ? await EditLineAsync(purchase, existing[lineId], item, returnedByLine.GetValueOrDefault(lineId),
                            Removable, inUse, moves, newSerialNumbers, now, cancellationToken)
                        : await AddLineAsync(purchase, item, moves, newSerialNumbers, now, cancellationToken);
                    if (error != null) return error;
                }

                var liveLines = purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected).ToList();
                if (liveLines.Count == 0)
                    return Error(400, "A purchase needs at least one line. To send everything back, use Purchase Return.");

                if (newSerialNumbers.Count > 0)
                {
                    var taken = await PurchaseSerials.FindExistingAsync(_dbContext, newSerialNumbers, null, cancellationToken);
                    if (taken.Count > 0)
                        return Error(400, $"These serial numbers already exist in inventory or on another purchase order: {string.Join(", ", taken)}.");
                }

                var subTotal = liveLines.Sum(d => d.TotalAmount);
                if (request.DiscountAmount > subTotal)
                    return Error(400, $"DiscountAmount {request.DiscountAmount} exceeds the subtotal {subTotal}.");
                var newTotal = subTotal - request.DiscountAmount;
                var returned = purchase.ReturnedAmount;
                if (newTotal < returned)
                    return Error(400, $"The new total {newTotal:0.00} is less than the {returned:0.00} already returned to the supplier.");

                // Stock moves by the net difference only, each as a traceable Adjustment row.
                var stockMoves = new List<object>();
                var stocks = new Dictionary<int, Stock>();
                foreach (var move in moves.Where(m => m.Delta != 0))
                {
                    var stock = await ReturnMath.GetOrCreateStockAsync(_dbContext, stocks, purchase.Branch, move.Variant, cancellationToken);
                    if (move.Delta < 0 && stock.CurrentStock < -move.Delta)
                        return Error(400, $"Cannot take {-move.Delta} unit(s) of '{move.Variant.Product.ProductName}' off this bill: only {stock.CurrentStock} on hand, the rest was sold or moved. Use Purchase Return for goods that have to go back.");

                    stock.CurrentStock += move.Delta;
                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = purchase.BranchId,
                        ProductVariantId = move.Variant.Id,
                        SupplierPurchaseDetails = move.Detail,
                        TransactionType = InventoryTxnType.Adjustment,
                        QuantityIn = move.Delta > 0 ? move.Delta : 0,
                        QuantityOut = move.Delta < 0 ? -move.Delta : 0,
                        BalanceAfter = stock.CurrentStock,
                        TransactionDate = now,
                        Branch = purchase.Branch,
                        ProductVariant = move.Variant,
                    }, cancellationToken);
                    stockMoves.Add(new
                    {
                        variantId = move.Variant.Id,
                        sku = move.Variant.SKU,
                        productName = move.Variant.Product.ProductName,
                        change = move.Delta,
                        balanceAfter = stock.CurrentStock,
                    });
                }

                // ---------------------------------------------------------------- Phase B: undo settlements
                // Linked customer first, then the supplier (lock order).
                var supplier = purchase.Supplier;
                var linkedCustomerId = await _dbContext.Customers
                    .Where(c => c.SupplierId == purchase.SupplierId)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (linkedCustomerId is int customerId)
                    await _dbContext.LockRowAsync<Customer>(customerId, cancellationToken);
                await _dbContext.LockRowAsync<Supplier>(supplier.Id, cancellationToken);
                var balance = await _dbContext.SupplierTransactions
                    .Where(t => t.SupplierId == supplier.Id)
                    .GetLatestBalanceAsync(cancellationToken);

                var allocations = await _dbContext.SupplierPurchasePayments.IgnoreQueryFilters()
                    .Where(a => a.SupplierPurchaseId == purchase.Id)
                    .Select(a => new { a.SupplierPaymentId, a.Amount, a.SupplierPayment.PaymentMethod })
                    .ToListAsync(cancellationToken);

                decimal settlementReversed = 0;
                var settled = allocations.Where(a => a.PaymentMethod == PartySettlement.PaymentMethod).Sum(a => a.Amount);
                if (purchase.PurchaseType == PurchaseType.Debit && settled > 0)
                {
                    var reversal = await new CrossLedgerSettlement(_dbContext).ReverseOnPurchaseAsync(
                        purchase, settled, balance, now, $"Settlement undone: bill {purchase.InvoiceNumber} edited", cancellationToken);
                    settlementReversed = reversal.Reversed;
                    balance = reversal.SupplierBalance;
                }

                // ---------------------------------------------------------------- Phase D: redo
                var difference = newTotal - purchase.TotalAmount;
                if (difference != 0)
                {
                    balance += difference;
                    await _dbContext.SupplierTransactions.AddAsync(
                        SupplierTransaction.ForPurchaseEdit(purchase, supplier, difference, now, balance), cancellationToken);
                }

                purchase.DiscountAmount = request.DiscountAmount;
                purchase.TotalAmount = newTotal;
                purchase.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();
                purchase.DueAmount = newTotal - purchase.PaidAmount - returned;
                purchase.UpDatedAt = now;

                // Payments to the supplier are never deleted. If they now exceed what the bill needs,
                // the surplus is released from it (the payment stays) and remains our advance with
                // the supplier, unless the editor says the supplier paid it back.
                decimal cashReleased = 0, cashRefunded = 0, cashReapplied = 0;
                if (purchase.DueAmount < 0)
                {
                    var excess = -purchase.DueAmount;
                    cashReleased = excess;

                    if (request.RefundExcess)
                    {
                        var refund = new SupplierPayment
                        {
                            SupplierId = purchase.SupplierId,
                            BranchId = purchase.BranchId,
                            Amount = -excess,
                            PaymentDate = now,
                            PaymentMethod = "Refund",
                            Remarks = $"Refund from supplier for edit of bill {purchase.InvoiceNumber}",
                            Supplier = supplier,
                            Branch = purchase.Branch,
                        };
                        await _dbContext.SupplierPayments.AddAsync(refund, cancellationToken);
                        await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                        {
                            Amount = -excess,
                            AllocationDate = now,
                            SupplierPurchase = purchase,
                            SupplierPayment = refund,
                        }, cancellationToken);
                        balance += excess;
                        await _dbContext.SupplierTransactions.AddAsync(
                            SupplierTransaction.ForEditRefund(refund, supplier, excess, balance, purchase), cancellationToken);
                        cashRefunded = excess;
                    }
                    else
                    {
                        var net = allocations
                            .Where(a => a.PaymentMethod != PartySettlement.PaymentMethod && a.PaymentMethod != "Refund")
                            .GroupBy(a => a.SupplierPaymentId)
                            .Select(g => new { PaymentId = g.Key, Amount = g.Sum(a => a.Amount) })
                            .Where(x => x.Amount > 0)
                            .OrderByDescending(x => x.PaymentId)
                            .ToList();
                        var left = excess;
                        foreach (var p in net)
                        {
                            if (left <= 0) break;
                            var take = Math.Min(p.Amount, left);
                            var payment = await _dbContext.SupplierPayments.IgnoreQueryFilters()
                                .FirstAsync(x => x.Id == p.PaymentId, cancellationToken);
                            await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                            {
                                Amount = -take,
                                AllocationDate = now,
                                SupplierPurchase = purchase,
                                SupplierPayment = payment,
                            }, cancellationToken);
                            left -= take;
                        }
                        if (left > 0)
                            return Error(500, "The bill is overpaid by more than its cash payments cover; nothing was changed.");
                    }

                    purchase.PaidAmount -= excess;
                    purchase.DueAmount = 0;
                }
                else if (purchase.DueAmount > 0)
                {
                    // Money released from this bill by an earlier edit goes back on it first.
                    var pool = await _dbContext.SupplierPayments.IgnoreQueryFilters()
                        .Include(p => p.SupplierPurchasePayments)
                        .Where(p => p.SupplierId == purchase.SupplierId
                                    && p.Amount > 0
                                    && p.PaymentMethod != PartySettlement.PaymentMethod
                                    && p.PaymentMethod != "Refund"
                                    && p.SupplierPurchasePayments.Any(a => a.SupplierPurchaseId == purchase.Id))
                        .OrderBy(p => p.Id)
                        .ToListAsync(cancellationToken);
                    foreach (var payment in pool)
                    {
                        if (purchase.DueAmount <= 0) break;
                        var free = payment.Amount - payment.SupplierPurchasePayments.Sum(a => a.Amount);
                        var take = Math.Min(free, purchase.DueAmount);
                        if (take <= 0) continue;
                        await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                        {
                            Amount = take,
                            AllocationDate = now,
                            SupplierPurchase = purchase,
                            SupplierPayment = payment,
                        }, cancellationToken);
                        purchase.ApplyPayment(take);
                        cashReapplied += take;
                    }
                }

                // New money. A Cash bill is settled only by real payments, so it has to be exact.
                var additional = request.AdditionalPayment ?? 0m;
                if (purchase.PurchaseType == PurchaseType.Cash)
                {
                    if (additional != purchase.DueAmount)
                        return Error(400, purchase.DueAmount > 0
                            ? $"This edit adds {purchase.DueAmount:0.00} to a Cash purchase: pay exactly that as the additional payment."
                            : "No additional payment is needed for this edit.");
                }
                else if (additional > purchase.DueAmount)
                {
                    return Error(400, $"The additional payment {additional:0.00} exceeds what is due on the bill ({purchase.DueAmount:0.00}).");
                }

                if (additional > 0)
                {
                    var cash = new SupplierPayment
                    {
                        SupplierId = purchase.SupplierId,
                        BranchId = purchase.BranchId,
                        Amount = additional,
                        PaymentDate = now,
                        PaymentMethod = PurchaseType.Cash.ToString(),
                        Remarks = $"Paid with edit of bill {purchase.InvoiceNumber}",
                        Supplier = supplier,
                        Branch = purchase.Branch,
                    };
                    await _dbContext.SupplierPayments.AddAsync(cash, cancellationToken);
                    purchase.ApplyPayment(additional);
                    await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                    {
                        Amount = additional,
                        AllocationDate = now,
                        SupplierPurchase = purchase,
                        SupplierPayment = cash,
                    }, cancellationToken);
                    balance -= additional;
                    await _dbContext.SupplierTransactions.AddAsync(
                        SupplierTransaction.ForPayment(cash, supplier, balance, purchase), cancellationToken);
                }

                // A Credit bill from a party that is also our customer: settle again on what is still due.
                decimal settlementRedone = 0;
                if (purchase.PurchaseType == PurchaseType.Debit && purchase.DueAmount > 0)
                {
                    var settlement = await new CrossLedgerSettlement(_dbContext).SettlePurchaseAsync(purchase, supplier, cancellationToken);
                    settlementRedone = settlement?.Amount ?? 0m;
                }

                // ---------------------------------------------------------------- Audit
                var afterLines = liveLines.Select(Snapshot).ToList();
                await _dbContext.PurchaseEdits.AddAsync(new PurchaseEdit
                {
                    PurchaseId = purchase.Id,
                    SupplierPurchase = purchase,
                    Reason = request.Reason.Trim(),
                    OldDiscountAmount = old.DiscountAmount, OldTotalAmount = old.TotalAmount,
                    OldPaidAmount = old.PaidAmount, OldDueAmount = old.DueAmount,
                    NewDiscountAmount = purchase.DiscountAmount, NewTotalAmount = purchase.TotalAmount,
                    NewPaidAmount = purchase.PaidAmount, NewDueAmount = purchase.DueAmount,
                    ReturnedAmount = returned,
                    SettlementReversed = settlementReversed,
                    SettlementRedone = settlementRedone,
                    CashReleased = cashReleased,
                    CashRefunded = cashRefunded,
                    CashReapplied = cashReapplied,
                    AdditionalPayment = additional,
                    ChangesJson = JsonSerializer.Serialize(new
                    {
                        before = beforeLines,
                        after = afterLines,
                        removedLineIds = removed.Select(r => r.Id),
                        stock = stockMoves,
                    }, Json),
                }, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var advance = cashReleased - cashRefunded;
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Purchase {purchase.InvoiceNumber} updated: total {old.TotalAmount:0.00} -> {purchase.TotalAmount:0.00}"
                              + (purchase.DueAmount > 0 ? $", {purchase.DueAmount:0.00} due" : "")
                              + (settlementRedone > 0 ? $", {settlementRedone:0.00} settled against what this party owes us as a customer" : "")
                              + (advance > 0 ? $", {advance:0.00} kept as our advance with the supplier" : "")
                              + (cashRefunded > 0 ? $", {cashRefunded:0.00} refunded by the supplier in cash" : "")
                              + ".",
                    Data = new
                    {
                        purchase.Id,
                        purchase.InvoiceNumber,
                        OldTotal = old.TotalAmount,
                        NewTotal = purchase.TotalAmount,
                        Returned = returned,
                        purchase.PaidAmount,
                        purchase.DueAmount,
                        SettlementReversed = settlementReversed,
                        SettlementRedone = settlementRedone,
                        CashReleased = cashReleased,
                        CashRefunded = cashRefunded,
                        CashReapplied = cashReapplied,
                        AdditionalPayment = additional,
                        Advance = advance,
                    }
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Purchase edit {PurchaseId} lost a uniqueness race", request.Id);
                return Error(409, "A serial number on this purchase was taken by another user a moment ago. Nothing was changed - check the serials and try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error editing purchase {PurchaseId}", request.Id);
                return Error(500, "An error occurred while editing the purchase. Nothing was changed.");
            }
        }

        // ------------------------------------------------------------------ lines

        private async Task<Result?> EditLineAsync(
            SupplierPurchase purchase, SupplierPurchaseDetails line, EditPurchaseItemRequest item, int returnedQty,
            Func<ProductSerial, bool> removable, HashSet<int> inUse, List<StockMove> moves, List<string> newSerialNumbers,
            DateTime now, CancellationToken cancellationToken)
        {
            var variant = line.ProductVariant;
            var name = variant.Product.ProductName;
            if (item.ProductVariantId != line.ProductVariantId)
                return Error(400, $"'{name}': a line's product cannot be changed. Remove the line and add the new product.");

            var price = decimal.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero);
            if (returnedQty > 0 && price != line.UnitPrice)
                return Error(400, $"'{name}' has returns booked at {line.UnitPrice:0.00}, so its cost cannot be changed.");
            if (item.Quantity < returnedQty)
                return Error(400, $"'{name}': the quantity cannot be less than the {returnedQty} already returned to the supplier.");

            var received = line.ReceivedQuantity ?? 0;
            if (variant.IsSerialized)
            {
                var desired = PurchaseSerials.Clean(item.SerialNumbers);
                if (desired.Count != item.Quantity)
                    return Error(400, $"'{name}' is serial-tracked: give exactly {item.Quantity} serial number(s), one per unit ({desired.Count} given).");
                if (desired.Any(s => s.Length > PurchaseSerials.MaxLength))
                    return Error(400, $"A serial number on '{name}' is longer than {PurchaseSerials.MaxLength} characters.");
                var dup = desired.GroupBy(s => s, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
                if (dup != null)
                    return Error(400, $"Serial number '{dup.Key}' is listed more than once on '{name}'.");

                var current = line.ProductSerials.Where(s => s.Status != SerialStatus.Ordered).ToList();
                var wanted = new HashSet<string>(desired, StringComparer.OrdinalIgnoreCase);
                var dropped = current.Where(s => !wanted.Contains(s.SerialNumber)).ToList();
                var stuck = dropped.FirstOrDefault(s => !removable(s));
                if (stuck != null)
                    return Error(400, $"'{name}': serial {stuck.SerialNumber} is {SerialReason(stuck, inUse)}, so it cannot be removed. Keep it on the bill, or use Purchase Return.");

                var have = new HashSet<string>(current.Select(s => s.SerialNumber), StringComparer.OrdinalIgnoreCase);
                var added = desired.Where(s => !have.Contains(s)).ToList();

                _dbContext.ProductSerials.RemoveRange(dropped);
                foreach (var sn in added)
                {
                    line.ProductSerials.Add(new ProductSerial
                    {
                        ProductVariantId = variant.Id,
                        BranchId = purchase.BranchId,
                        SerialNumber = sn,
                        Status = SerialStatus.InStock,
                        WarrantyDays = item.WarrantyDays,
                        ReceivedDate = now,
                        ProductVariant = variant,
                        SupplierPurchaseDetails = line,
                        Branch = purchase.Branch,
                    });
                    newSerialNumbers.Add(sn);
                }
                if (added.Count - dropped.Count != 0)
                    moves.Add(new StockMove(line, variant, added.Count - dropped.Count));
            }
            else
            {
                if (item.SerialNumbers is { Count: > 0 } && PurchaseSerials.Clean(item.SerialNumbers).Count > 0)
                    return Error(400, $"'{name}' is not serial-tracked, so serial numbers cannot be entered for it.");
                if (item.Quantity != received)
                    moves.Add(new StockMove(line, variant, item.Quantity - received));
            }

            // Units still in stock carry the new warranty term; units already sold keep what they were sold with.
            if (item.WarrantyDays != line.WarrantyDays)
            {
                line.WarrantyDays = item.WarrantyDays;
                foreach (var s in line.ProductSerials.Where(s => s.Status == SerialStatus.InStock))
                    s.WarrantyDays = item.WarrantyDays;
            }

            line.OrderedQuantity = item.Quantity;
            line.ReceivedQuantity = item.Quantity;
            line.UnitPrice = price;
            line.TotalAmount = price * item.Quantity;
            line.Status = LineStatus.Received;
            return null;
        }

        private async Task<Result?> AddLineAsync(
            SupplierPurchase purchase, EditPurchaseItemRequest item, List<StockMove> moves, List<string> newSerialNumbers,
            DateTime now, CancellationToken cancellationToken)
        {
            var variant = await _dbContext.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, cancellationToken);
            if (variant == null)
                return Error(404, $"Product variant with id {item.ProductVariantId} not found.");
            var name = variant.Product.ProductName;

            var serials = PurchaseSerials.Clean(item.SerialNumbers);
            if (variant.IsSerialized)
            {
                if (serials.Count != item.Quantity)
                    return Error(400, $"'{name}' is serial-tracked: give exactly {item.Quantity} serial number(s), one per unit ({serials.Count} given).");
                if (serials.Any(s => s.Length > PurchaseSerials.MaxLength))
                    return Error(400, $"A serial number on '{name}' is longer than {PurchaseSerials.MaxLength} characters.");
                var dup = serials.GroupBy(s => s, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
                if (dup != null)
                    return Error(400, $"Serial number '{dup.Key}' is listed more than once on '{name}'.");
            }
            else if (serials.Count > 0)
            {
                return Error(400, $"'{name}' is not serial-tracked, so serial numbers cannot be entered for it.");
            }

            var price = decimal.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var detail = new SupplierPurchaseDetails
            {
                ProductVariantId = variant.Id,
                OrderedQuantity = item.Quantity,
                ReceivedQuantity = item.Quantity,
                UnitPrice = price,
                TotalAmount = price * item.Quantity,
                WarrantyDays = item.WarrantyDays,
                Status = LineStatus.Received,
                SupplierPurchase = purchase,
                ProductVariant = variant,
            };
            foreach (var sn in serials)
            {
                detail.ProductSerials.Add(new ProductSerial
                {
                    ProductVariantId = variant.Id,
                    BranchId = purchase.BranchId,
                    SerialNumber = sn,
                    Status = SerialStatus.InStock,
                    WarrantyDays = item.WarrantyDays,
                    ReceivedDate = now,
                    ProductVariant = variant,
                    SupplierPurchaseDetails = detail,
                    Branch = purchase.Branch,
                });
                newSerialNumbers.Add(sn);
            }
            purchase.SupplierPurchaseDetails.Add(detail);
            moves.Add(new StockMove(detail, variant, item.Quantity));
            return null;
        }

        // ------------------------------------------------------------------ helpers

        // Serials that a sale, a sales return, a warranty claim or a purchase return has touched:
        // they carry history and must not disappear from the record.
        private async Task<HashSet<int>> SerialsWithHistoryAsync(List<int> serialIds, CancellationToken cancellationToken)
        {
            var used = new HashSet<int>();
            if (serialIds.Count == 0) return used;

            used.UnionWith(await _dbContext.SaleDetails.IgnoreQueryFilters()
                .Where(d => d.ProductSerialId != null && serialIds.Contains(d.ProductSerialId.Value))
                .Select(d => d.ProductSerialId!.Value).ToListAsync(cancellationToken));
            used.UnionWith(await _dbContext.SalesReturnLines.IgnoreQueryFilters()
                .Where(l => l.ProductSerialId != null && serialIds.Contains(l.ProductSerialId.Value))
                .Select(l => l.ProductSerialId!.Value).ToListAsync(cancellationToken));
            used.UnionWith(await _dbContext.WarrantyClaims.IgnoreQueryFilters()
                .Where(c => serialIds.Contains(c.ProductSerialId))
                .Select(c => c.ProductSerialId).ToListAsync(cancellationToken));
            used.UnionWith(await _dbContext.PurchaseReturnSerials.IgnoreQueryFilters()
                .Where(r => serialIds.Contains(r.ProductSerialId))
                .Select(r => r.ProductSerialId).ToListAsync(cancellationToken));
            return used;
        }

        private static string SerialReason(ProductSerial s, HashSet<int> inUse) =>
            s.Status != SerialStatus.InStock ? $"{s.Status}" :
            inUse.Contains(s.Id) ? "already used on a sale, return or warranty claim" :
            "no longer in this warehouse";

        private static object Snapshot(SupplierPurchaseDetails d) => new
        {
            id = d.Id,
            productVariantId = d.ProductVariantId,
            productName = d.ProductVariant.Product.ProductName,
            sku = d.ProductVariant.SKU,
            quantity = d.ReceivedQuantity ?? d.OrderedQuantity,
            unitPrice = d.UnitPrice,
            totalAmount = d.TotalAmount,
            warrantyDays = d.WarrantyDays,
            serials = d.ProductSerials.Where(s => s.Status != SerialStatus.Ordered).Select(s => s.SerialNumber).OrderBy(s => s).ToList(),
        };

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
