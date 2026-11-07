using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using static Inventory_Management_System.Entities.Common.EntityConstant;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Command.ReceiveGoods
{
    public class ReceiveGoodsHandler(
        AppDbContext _dbContext,
        ILogger<ReceiveGoodsHandler> _logger
    ) : IRequestHandler<ReceiveGoodsCommand, Result>
    {
        public async Task<Result> Handle(ReceiveGoodsCommand request, CancellationToken cancellationToken)
        {
            // Lock the order first: a double click on "Receive" now waits here, then sees the
            // new status, instead of adding the same goods to stock twice.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.LockRowAsync<SupplierPurchase>(request.PurchaseOrderId, cancellationToken);

            var purchase = await _dbContext.SupplierPurchases
                .Include(p => p.Supplier)
                .Include(p => p.Branch)
                .Include(p => p.SupplierPurchaseDetails).ThenInclude(d => d.ProductVariant)
                .FirstOrDefaultAsync(
                    p => p.Id == request.PurchaseOrderId && p.IsActive != (int)EntityStatus.Deleted,
                    cancellationToken);

            if (purchase == null)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Error",
                    Message = "Purchase order not found."
                };
            if (purchase.Status == PurchaseStatus.Rejected)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "A rejected purchase order cannot be received."
                };
            if (purchase.Status == PurchaseStatus.Approved)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "This purchase order is already fully received."
                };

            if (request.Lines.GroupBy(l => l.SupplierPurchaseDetailsId).Any(g => g.Count() > 1))
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "The same line is listed twice in this receipt."
                };

            // Serials entered when the order was raised (status Ordered), per line. Approval turns
            // these into stock; nobody types them again.
            var lineIds = purchase.SupplierPurchaseDetails.Select(d => d.Id).ToList();
            var pendingByLine = (await _dbContext.ProductSerials
                    .IgnoreQueryFilters()
                    .Where(s => lineIds.Contains(s.SupplierPurchaseDetailsId) && s.Status == SerialStatus.Ordered)
                    .OrderBy(s => s.Id)
                    .ToListAsync(cancellationToken))
                .GroupBy(s => s.SupplierPurchaseDetailsId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var plan = new List<(SupplierPurchaseDetails Detail, int Qty, List<string> Serials, List<ProductSerial> Activate)>();
            var allSerials = new List<string>();

            foreach (var line in request.Lines)
            {
                var detail = purchase.SupplierPurchaseDetails.FirstOrDefault(d => d.Id == line.SupplierPurchaseDetailsId);
                if (detail == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Line {line.SupplierPurchaseDetailsId} does not belong to this purchase order."
                    };
                if (detail.Status == LineStatus.Rejected)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Line {line.SupplierPurchaseDetailsId} is rejected and cannot be received."
                    };
                if (detail.Status == LineStatus.Received)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Line {line.SupplierPurchaseDetailsId} is already received."
                    };

                // More than was ordered can never be received on a line.
                var remaining = detail.OrderedQuantity - (detail.ReceivedQuantity ?? 0);

                if (detail.ProductVariant.IsSerialized && pendingByLine.TryGetValue(detail.Id, out var pending))
                {
                    // Pre-entered serials: the caller says which of them arrived (all of them by
                    // default), or just a quantity, in which case the oldest ones are taken.
                    var requested = PurchaseSerials.Clean(line.SerialNumbers);
                    List<ProductSerial> activate;
                    if (requested.Count > 0)
                    {
                        var unknown = requested.Where(r => pending.All(p => p.SerialNumber != r)).ToList();
                        if (unknown.Count > 0)
                            return new Result
                            {
                                IsSuccess = false,
                                StatusCode = 400,
                                Status = "Error",
                                Message = $"Line {line.SupplierPurchaseDetailsId}: these serial numbers are not on this purchase order: {string.Join(", ", unknown)}."
                            };
                        activate = pending.Where(p => requested.Contains(p.SerialNumber)).ToList();
                    }
                    else
                    {
                        var qty = line.ReceivedQuantity ?? pending.Count;
                        if (qty <= 0)
                            return new Result
                            {
                                IsSuccess = false,
                                StatusCode = 400,
                                Status = "Error",
                                Message = $"A received quantity greater than 0 is required for line {line.SupplierPurchaseDetailsId}."
                            };
                        if (qty > pending.Count)
                            return new Result
                            {
                                IsSuccess = false,
                                StatusCode = 400,
                                Status = "Error",
                                Message = $"Line {line.SupplierPurchaseDetailsId}: {qty} received, but only {pending.Count} serial number(s) are waiting."
                            };
                        activate = pending.Take(qty).ToList();
                    }

                    if (activate.Count > remaining)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Line {line.SupplierPurchaseDetailsId}: {activate.Count} received, but only {remaining} still to receive."
                        };

                    plan.Add((detail, activate.Count, [], activate));
                }
                else if (detail.ProductVariant.IsSerialized)
                {
                    // An order raised before serials moved to order entry: typed in here, as before.
                    var serials = (line.SerialNumbers ?? [])
                        .Select(s => s?.Trim() ?? string.Empty)
                        .Where(s => s.Length > 0)
                        .ToList();

                    if (serials.Count == 0)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Serial numbers are required for serialized line {line.SupplierPurchaseDetailsId}."
                        };

                    if (serials.Count > remaining)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Line {line.SupplierPurchaseDetailsId}: {serials.Count} serial numbers sent, but only {remaining} still to receive."
                        };

                    allSerials.AddRange(serials);
                    plan.Add((detail, serials.Count, serials, []));
                }
                else
                {
                    var qty = line.ReceivedQuantity ?? 0;
                    if (qty <= 0)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"A received quantity greater than 0 is required for line {line.SupplierPurchaseDetailsId}."
                        };

                    if (qty > remaining)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Line {line.SupplierPurchaseDetailsId}: {qty} received, but only {remaining} still to receive."
                        };

                    plan.Add((detail, qty, [], []));
                }
            }

            var dupInRequest = allSerials.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupInRequest.Count > 0)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"Duplicate serial numbers in this receipt: {string.Join(", ", dupInRequest)}."
                };

            if (allSerials.Count > 0)
            {
                // Serial numbers are unique across all branches, so check them all.
                var existing = await _dbContext.ProductSerials
                    .IgnoreQueryFilters()
                    .Where(s => allSerials.Contains(s.SerialNumber))
                    .Select(s => s.SerialNumber)
                    .ToListAsync(cancellationToken);
                if (existing.Count > 0)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"These serial numbers already exist: {string.Join(", ", existing)}."
                    };
            }

            // Approval is the moment the order settles, so the payment type is decided here and
            // checked before anything is written. An order raised before the type moved to this
            // step keeps whatever it already carries unless the caller picks a new one.
            var plannedQuantities = plan.ToDictionary(p => p.Detail.Id, p => p.Qty);
            var openLines = purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected).ToList();
            int ReceivedAfter(SupplierPurchaseDetails d) => (d.ReceivedQuantity ?? 0) + plannedQuantities.GetValueOrDefault(d.Id);

            // Closing short completes the order with whatever has arrived.
            var willComplete = request.CloseRemaining
                ? openLines.Any(d => ReceivedAfter(d) > 0)
                : openLines.Count > 0 && openLines.All(d => ReceivedAfter(d) >= d.OrderedQuantity);

            if (request.CloseRemaining && !willComplete)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Nothing has been received on this order, so it cannot be closed. Reject it instead."
                };

            // What the supplier is owed once this receipt is saved: the full order, or only
            // what really arrived when the rest is cancelled.
            // The overall discount was agreed on the whole order, so a short close scales it
            // down to the share that actually arrived.
            var finalDiscount = purchase.DiscountAmount;
            var finalTotal = purchase.TotalAmount;
            if (request.CloseRemaining)
            {
                var orderedSubTotal = purchase.TotalAmount + purchase.DiscountAmount;
                var receivedSubTotal = decimal.Round(openLines.Sum(d => ReceivedAfter(d) * d.UnitPrice), 2, MidpointRounding.AwayFromZero);
                finalDiscount = orderedSubTotal > 0
                    ? decimal.Round(purchase.DiscountAmount * receivedSubTotal / orderedSubTotal, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                finalTotal = receivedSubTotal - finalDiscount;
            }
            var finalDue = finalTotal - purchase.PaidAmount;

            PurchaseType? approvalPaymentType = null;
            decimal settlementAmount = 0;

            if (willComplete)
            {
                if (!string.IsNullOrWhiteSpace(request.PaymentType))
                {
                    if (!PurchasePaymentTypes.TryParse(request.PaymentType, out var selectedType))
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Payment type must be one of: {PurchasePaymentTypes.Allowed}."
                        };

                    approvalPaymentType = selectedType;
                }
                else
                {
                    approvalPaymentType = purchase.PurchaseType;
                }

                if (approvalPaymentType == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"A payment type is required to approve this purchase order. Choose one of: {PurchasePaymentTypes.Allowed}."
                    };

                if (approvalPaymentType == PurchaseType.Cash)
                {
                    if (request.PaymentAmount.HasValue)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = "A Cash order is settled in full on approval, so a payment amount cannot be sent with it."
                        };

                    settlementAmount = finalDue;
                }
                else
                {
                    settlementAmount = request.PaymentAmount ?? 0m;

                    if (settlementAmount > finalDue)
                        return new Result
                        {
                            IsSuccess = false,
                            StatusCode = 400,
                            Status = "Error",
                            Message = $"Payment {settlementAmount} exceeds the order total {finalDue}."
                        };
                }
            }
            else if (request.PaymentAmount.HasValue)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "A payment can only be taken on the receipt that completes the purchase order."
                };
            }

            try
            {
                await _dbContext.LockStocksAsync(
                    [purchase.BranchId],
                    plan.Select(p => p.Detail.ProductVariantId),
                    cancellationToken);

                var now = DateTime.UtcNow;
                var serialsCreated = 0;
                var stockByVariant = new Dictionary<int, Stock>();

                var lineResults = new List<ReceivedLineResponse>();

                foreach (var (detail, qty, serials, activate) in plan)
                {
                    var variant = detail.ProductVariant;

                    foreach (var serial in activate)
                    {
                        serial.Status = SerialStatus.InStock;
                        serial.BranchId = purchase.BranchId;
                        serial.WarrantyDays = detail.WarrantyDays;
                        serial.ReceivedDate = now;
                        serialsCreated++;
                    }

                    foreach (var sn in serials)
                    {
                        await _dbContext.ProductSerials.AddAsync(new ProductSerial
                        {
                            ProductVariantId = variant.Id,
                            SupplierPurchaseDetailsId = detail.Id,
                            BranchId = purchase.BranchId,
                            SerialNumber = sn,
                            Status = SerialStatus.InStock,
                            WarrantyDays = detail.WarrantyDays,
                            ReceivedDate = now,
                            ProductVariant = variant,
                            SupplierPurchaseDetails = detail,
                            Branch = purchase.Branch,
                        }, cancellationToken);
                        serialsCreated++;
                    }

                    detail.ApplyReceipt(qty);

                    var stock = await GetOrCreateStockAsync(stockByVariant, purchase, variant, cancellationToken);
                    var newBalance = stock.CurrentStock + qty;
                    stock.CurrentStock = newBalance;

                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = purchase.BranchId,
                        ProductVariantId = variant.Id,
                        SupplierPurchaseDetailsId = detail.Id,
                        TransactionType = InventoryTxnType.PurchaseIn,
                        QuantityIn = qty,
                        QuantityOut = 0,
                        BalanceAfter = newBalance,
                        TransactionDate = now,
                        Branch = purchase.Branch,
                        ProductVariant = variant,
                        SupplierPurchaseDetails = detail,
                    }, cancellationToken);

                    lineResults.Add(new ReceivedLineResponse(
                        detail.Id, detail.OrderedQuantity, detail.ReceivedQuantity ?? 0, detail.Status.ToString()));
                }

                if (request.CloseRemaining)
                {
                    foreach (var detail in purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected))
                        detail.CloseShort();

                    // The rest will never arrive, so its pre-entered serials are freed.
                    var activated = plan.SelectMany(p => p.Activate).ToHashSet();
                    _dbContext.ProductSerials.RemoveRange(
                        pendingByLine.Values.SelectMany(v => v).Where(s => !activated.Contains(s)));

                    purchase.DiscountAmount = finalDiscount;
                    purchase.TotalAmount = finalTotal;
                    purchase.DueAmount = finalTotal - purchase.PaidAmount;
                }

                var nonRejected = purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected).ToList();
                if (nonRejected.Count > 0 && nonRejected.All(d => d.Status == LineStatus.Received))
                    purchase.Status = PurchaseStatus.Approved;
                else if (nonRejected.Any(d => d.Status is LineStatus.Received or LineStatus.PartiallyReceived))
                    purchase.Status = PurchaseStatus.PartiallyReceived;

                SupplierPayment? payment = null;

                if (purchase.Status == PurchaseStatus.Approved)
                {
                    purchase.PurchaseType = approvalPaymentType;

                    await _dbContext.LockRowAsync<Supplier>(purchase.SupplierId, cancellationToken);
                    var runningBalance = await _dbContext.SupplierTransactions
                        .Where(t => t.SupplierId == purchase.SupplierId)
                        .GetLatestBalanceAsync(cancellationToken);
                    runningBalance += purchase.TotalAmount;

                    var purchaseTxn = SupplierTransaction.ForPurchase(purchase, purchase.Supplier, runningBalance);
                    await _dbContext.SupplierTransactions.AddAsync(purchaseTxn, cancellationToken);

                    // Cash settles the whole order here and now. Debit settles only what was
                    // paid up front, if anything, and leaves the rest sitting on the supplier
                    // account to be paid off later through create-supplier-payment.
                    var paymentAmount = Math.Min(settlementAmount, purchase.DueAmount);
                    if (paymentAmount > 0)
                    {
                        purchase.ApplyPayment(paymentAmount);

                        var paymentDate = request.PaymentDate ?? now;
                        payment = new SupplierPayment
                        {
                            SupplierId = purchase.SupplierId,
                            BranchId = purchase.BranchId,
                            Amount = paymentAmount,
                            PaymentDate = paymentDate,
                            PaymentMethod = PurchaseType.Cash.ToString(),
                            Supplier = purchase.Supplier,
                            Branch = purchase.Branch,
                        };
                        await _dbContext.SupplierPayments.AddAsync(payment, cancellationToken);

                        await _dbContext.SupplierPurchasePayments.AddAsync(new SupplierPurchasePayment
                        {
                            Amount = paymentAmount,
                            AllocationDate = paymentDate,
                            SupplierPurchase = purchase,
                            SupplierPayment = payment,
                        }, cancellationToken);

                        runningBalance -= paymentAmount;

                        var paymentTxn = SupplierTransaction.ForPayment(payment, purchase.Supplier, runningBalance, purchase);
                        await _dbContext.SupplierTransactions.AddAsync(paymentTxn, cancellationToken);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new ReceiveGoodsResponse(
                    purchase.Id, purchase.Status.ToString(), purchase.PurchaseType?.ToString(), serialsCreated,
                    purchase.PaidAmount, purchase.DueAmount, lineResults,
                    payment == null ? null : new ReceiveGoodsPaymentResponse(payment.Id, payment.Amount, payment.PaymentDate, payment.PaymentMethod));
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Goods received successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error receiving goods for purchase order {Id}", request.PurchaseOrderId);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while receiving goods."
                };
            }
        }

        private async Task<Stock> GetOrCreateStockAsync(
            Dictionary<int, Stock> cache,
            SupplierPurchase purchase,
            ProductVariant variant,
            CancellationToken ct
        )
        {
            if (cache.TryGetValue(variant.Id, out var cached))
                return cached;

            var stock = await _dbContext.Stocks
                .FirstOrDefaultAsync(s => s.BranchId == purchase.BranchId && s.ProductVariantId == variant.Id, ct);

            if (stock == null)
            {
                stock = new Stock
                {
                    BranchId = purchase.BranchId,
                    ProductVariantId = variant.Id,
                    CurrentStock = 0,
                    Branch = purchase.Branch,
                    ProductVariant = variant,
                };
                await _dbContext.Stocks.AddAsync(stock, ct);
            }

            cache[variant.Id] = stock;
            return stock;
        }
    }
}
