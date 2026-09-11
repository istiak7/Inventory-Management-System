using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LedgerExtensions;
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

            var plan = new List<(SupplierPurchaseDetails Detail, int Qty, List<string> Serials)>();
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

                if (detail.ProductVariant.IsSerialized)
                {
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

                    allSerials.AddRange(serials);
                    plan.Add((detail, serials.Count, serials));
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

                    plan.Add((detail, qty, []));
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
                var existing = await _dbContext.ProductSerials
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

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var serialsCreated = 0;
                var stockByVariant = new Dictionary<int, Stock>();

                var lineResults = new List<ReceivedLineResponse>();

                foreach (var (detail, qty, serials) in plan)
                {
                    var variant = detail.ProductVariant;

                    foreach (var sn in serials)
                    {
                        await _dbContext.ProductSerials.AddAsync(new ProductSerial
                        {
                            ProductVariantId = variant.Id,
                            SupplierPurchaseDetailsId = detail.Id,
                            BranchId = purchase.BranchId,
                            SerialNumber = sn,
                            Status = SerialStatus.InStock,
                            WarrantyMonths = detail.WarrantyMonths,
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

                var nonRejected = purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected).ToList();
                if (nonRejected.Count > 0 && nonRejected.All(d => d.Status == LineStatus.Received))
                    purchase.Status = PurchaseStatus.Approved;
                else if (nonRejected.Any(d => d.Status is LineStatus.Received or LineStatus.PartiallyReceived))
                    purchase.Status = PurchaseStatus.PartiallyReceived;

                SupplierPayment? payment = null;

                if (purchase.Status == PurchaseStatus.Approved)
                {
                    var runningBalance = await _dbContext.SupplierTransactions
                        .Where(t => t.SupplierId == purchase.SupplierId)
                        .GetLatestBalanceAsync(cancellationToken);
                    runningBalance += purchase.TotalAmount;

                    var purchaseTxn = SupplierTransaction.ForPurchase(purchase, purchase.Supplier, runningBalance);
                    await _dbContext.SupplierTransactions.AddAsync(purchaseTxn, cancellationToken);

                    // Cash settles the whole order here and now; Debit leaves the full amount
                    // sitting on the supplier account, to be paid off later through
                    // create-supplier-payment like any other outstanding balance.
                    if (purchase.PurchaseType == PurchaseType.Cash && purchase.DueAmount > 0)
                    {
                        var paymentAmount = purchase.DueAmount;
                        purchase.SettleInFull();

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
                    purchase.Id, purchase.Status.ToString(), serialsCreated,
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
