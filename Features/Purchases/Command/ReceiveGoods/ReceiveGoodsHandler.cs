using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
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
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken);

            if (purchase == null)
                return Error(404, "Purchase order not found.");
            if (purchase.Status == PurchaseStatus.Rejected)
                return Error(400, "A rejected purchase order cannot be received.");
            if (purchase.Status == PurchaseStatus.Approved)
                return Error(400, "This purchase order is already fully received.");

            // ---- Validate every line up front (no partial side effects before this passes) ----
            var plan = new List<(SupplierPurchaseDetails Detail, int Qty, List<string> Serials)>();
            var allSerials = new List<string>();

            foreach (var line in request.Lines)
            {
                var detail = purchase.SupplierPurchaseDetails.FirstOrDefault(d => d.Id == line.SupplierPurchaseDetailsId);
                if (detail == null)
                    return Error(400, $"Line {line.SupplierPurchaseDetailsId} does not belong to this purchase order.");
                if (detail.Status == LineStatus.Rejected)
                    return Error(400, $"Line {line.SupplierPurchaseDetailsId} is rejected and cannot be received.");

                if (detail.ProductVariant.IsSerialized)
                {
                    var serials = (line.SerialNumbers ?? [])
                        .Select(s => s?.Trim() ?? string.Empty)
                        .Where(s => s.Length > 0)
                        .ToList();

                    if (serials.Count == 0)
                        return Error(400, $"Serial numbers are required for serialized line {line.SupplierPurchaseDetailsId}.");

                    allSerials.AddRange(serials);
                    plan.Add((detail, serials.Count, serials));
                }
                else
                {
                    var qty = line.ReceivedQuantity ?? 0;
                    if (qty <= 0)
                        return Error(400, $"A received quantity greater than 0 is required for line {line.SupplierPurchaseDetailsId}.");

                    plan.Add((detail, qty, []));
                }
            }

            // Serial uniqueness: within this request...
            var dupInRequest = allSerials.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupInRequest.Count > 0)
                return Error(400, $"Duplicate serial numbers in this receipt: {string.Join(", ", dupInRequest)}.");

            // ...and globally against everything already stored.
            if (allSerials.Count > 0)
            {
                var existing = await _dbContext.ProductSerials
                    .Where(s => allSerials.Contains(s.SerialNumber))
                    .Select(s => s.SerialNumber)
                    .ToListAsync(cancellationToken);
                if (existing.Count > 0)
                    return Error(400, $"These serial numbers already exist: {string.Join(", ", existing)}.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;
                var serialsCreated = 0;
                // Cache the Stock snapshot per variant so multiple lines for the same variant accumulate correctly.
                var stockByVariant = new Dictionary<int, Stock>();

                var lineResults = new List<ReceivedLineResponse>();

                foreach (var (detail, qty, serials) in plan)
                {
                    var variant = detail.ProductVariant;

                    // Serialized: one ProductSerial per unit, bound to this lot (cost + warranty lineage).
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

                    // Accumulate onto the lot and recompute the line status (over-receipt allowed).
                    detail.ApplyReceipt(qty);

                    // Stock snapshot (upsert) + inventory ledger row, kept consistent.
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

                // Header status derives from the non-rejected lines.
                var nonRejected = purchase.SupplierPurchaseDetails.Where(d => d.Status != LineStatus.Rejected).ToList();
                if (nonRejected.Count > 0 && nonRejected.All(d => d.Status == LineStatus.Received))
                    purchase.Status = PurchaseStatus.Approved;
                else if (nonRejected.Any(d => d.Status is LineStatus.Received or LineStatus.PartiallyReceived))
                    purchase.Status = PurchaseStatus.PartiallyReceived;

                // Ledger (money): the purchase debits the supplier account only once the order is
                // approved (every line fully received). Pending/rejected orders never touch the books.
                // Runs at most once: an already-approved order is rejected at the top of this handler.
                if (purchase.Status == PurchaseStatus.Approved)
                {
                    var runningBalance = await GetCurrentSupplierBalanceAsync(purchase.SupplierId, cancellationToken);
                    runningBalance += purchase.TotalAmount;
                    await _dbContext.SupplierTransactions.AddAsync(new SupplierTransaction
                    {
                        SupplierId = purchase.SupplierId,
                        TransactionType = "Purchase",
                        TransactionDate = now,
                        Debit = purchase.TotalAmount,
                        Credit = 0,
                        BalanceAfter = runningBalance,
                        SupplierPurchase = purchase,
                        Supplier = purchase.Supplier,
                    }, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new ReceiveGoodsResponse(purchase.Id, purchase.Status.ToString(), serialsCreated, lineResults);
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
                return Error(500, "An error occurred while receiving goods.");
            }
        }

        /// <summary>Get the (Branch, Variant) stock row from cache/DB, or create a fresh one, tracked.</summary>
        private async Task<Stock> GetOrCreateStockAsync(
            Dictionary<int, Stock> cache, SupplierPurchase purchase, ProductVariant variant, CancellationToken ct)
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

        /// <summary>Supplier's current overall balance = BalanceAfter of their latest transaction (0 if none).</summary>
        private async Task<decimal> GetCurrentSupplierBalanceAsync(int supplierId, CancellationToken cancellationToken)
        {
            return await _dbContext.SupplierTransactions
                .AsNoTracking()
                .Where(t => t.SupplierId == supplierId)
                .OrderByDescending(t => t.Id)
                .Select(t => t.BalanceAfter)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static Result Error(int code, string message) =>
            new() { IsSuccess = false, StatusCode = code, Status = "Error", Message = message };
    }
}
