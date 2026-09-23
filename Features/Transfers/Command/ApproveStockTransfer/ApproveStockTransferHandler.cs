using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory_Management_System.Features.Transfers.Command.ApproveStockTransfer
{
    public class ApproveStockTransferHandler(
        AppDbContext _dbContext,
        ILogger<ApproveStockTransferHandler> _logger
    ) : IRequestHandler<ApproveStockTransferCommand, Result>
    {
        public async Task<Result> Handle(ApproveStockTransferCommand request, CancellationToken cancellationToken)
        {
            // Lock the transfer first: a double click on "Approve" now waits here, then sees
            // the status is already Approved and stops, instead of moving the stock twice.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            await _dbContext.LockRowAsync<StockTransfer>(request.StockTransferId, cancellationToken);

            var transfer = await _dbContext.StockTransfers
                .Include(t => t.SourceBranch)
                .Include(t => t.DestinationBranch)
                .Include(t => t.StockTransferDetails).ThenInclude(d => d.ProductVariant).ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(t => t.Id == request.StockTransferId, cancellationToken);

            if (transfer == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Stock transfer not found." };

            if (transfer.Status is not (TransferStatus.Draft or TransferStatus.Pending))
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Only a draft or pending transfer can be approved." };

            try
            {
                await _dbContext.LockStocksAsync(
                    [transfer.SourceBranchId, transfer.DestinationBranchId],
                    transfer.StockTransferDetails.Select(d => d.ProductVariantId),
                    cancellationToken);

                var now = DateTime.UtcNow;
                var stockCache = new Dictionary<(int BranchId, int VariantId), Stock>();
                var claimedSerialIds = new HashSet<int>();
                var lineResponses = new List<StockTransferLineResponse>();

                foreach (var line in transfer.StockTransferDetails)
                {
                    var variant = line.ProductVariant;
                    var requestedSerials = JsonSerializer.Deserialize<List<string>>(line.RequestedSerialNumbersJson) ?? [];
                    var quantity = variant.IsSerialized ? requestedSerials.Count : line.Quantity;

                    var sourceStock = await GetTrackedStockAsync(stockCache, transfer.SourceBranchId, transfer.SourceBranch, variant, cancellationToken);
                    if (sourceStock.CurrentStock < quantity)
                        return new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}) at the source branch: {sourceStock.CurrentStock} on hand, {quantity} requested. Stock may have moved since this transfer was created." };

                    List<ProductSerial> serialsToMove = [];
                    if (variant.IsSerialized)
                    {
                        foreach (var serialNumber in requestedSerials)
                        {
                            var serial = await _dbContext.ProductSerials.FirstOrDefaultAsync(s =>
                                s.SerialNumber == serialNumber &&
                                s.ProductVariantId == variant.Id &&
                                s.BranchId == transfer.SourceBranchId &&
                                s.Status == SerialStatus.InStock, cancellationToken);

                            if (serial == null || !claimedSerialIds.Add(serial.Id))
                                return new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = $"Serial number '{serialNumber}' is no longer available in stock for '{variant.Product.ProductName}' at the source branch. It may have sold or moved since this transfer was created." };

                            serialsToMove.Add(serial);
                        }
                    }

                    var sourceBalance = sourceStock.CurrentStock - quantity;
                    sourceStock.CurrentStock = sourceBalance;

                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = transfer.SourceBranchId,
                        ProductVariantId = variant.Id,
                        TransactionType = InventoryTxnType.TransferOut,
                        QuantityIn = 0,
                        QuantityOut = quantity,
                        BalanceAfter = sourceBalance,
                        TransactionDate = now,
                        Branch = transfer.SourceBranch,
                        ProductVariant = variant,
                    }, cancellationToken);

                    var destinationStock = await GetTrackedStockAsync(stockCache, transfer.DestinationBranchId, transfer.DestinationBranch, variant, cancellationToken);
                    var destinationBalance = destinationStock.CurrentStock + quantity;
                    destinationStock.CurrentStock = destinationBalance;

                    await _dbContext.InventoryTransactions.AddAsync(new InventoryTransaction
                    {
                        BranchId = transfer.DestinationBranchId,
                        ProductVariantId = variant.Id,
                        TransactionType = InventoryTxnType.TransferIn,
                        QuantityIn = quantity,
                        QuantityOut = 0,
                        BalanceAfter = destinationBalance,
                        TransactionDate = now,
                        Branch = transfer.DestinationBranch,
                        ProductVariant = variant,
                    }, cancellationToken);

                    foreach (var serial in serialsToMove)
                    {
                        serial.BranchId = transfer.DestinationBranchId;
                        serial.StockTransferDetailsId = line.Id;
                    }

                    lineResponses.Add(new StockTransferLineResponse(
                        line.Id, variant.Id, variant.SKU, variant.Product.ProductName,
                        variant.IsSerialized, quantity, requestedSerials));
                }

                transfer.Status = TransferStatus.Approved;
                transfer.ApprovedAt = now;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var response = new StockTransferResponse(
                    transfer.Id, transfer.Reference, transfer.SourceBranchId, transfer.SourceBranch.Name,
                    transfer.DestinationBranchId, transfer.DestinationBranch.Name, transfer.Status.ToString(),
                    transfer.Notes, transfer.CreatedAt, transfer.SubmittedAt, transfer.ApprovedAt, transfer.RejectedAt,
                    lineResponses.Count, lineResponses.Sum(l => l.Quantity), lineResponses);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock transfer approved successfully", Data = response };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error approving stock transfer {Id}", request.StockTransferId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while approving the stock transfer." };
            }
        }

        private async Task<Stock> GetTrackedStockAsync(
            Dictionary<(int BranchId, int VariantId), Stock> cache,
            int branchId,
            Branch branch,
            ProductVariant variant,
            CancellationToken cancellationToken
        )
        {
            var key = (branchId, variant.Id);
            if (cache.TryGetValue(key, out var cached))
                return cached;

            var stock = await _dbContext.Stocks
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.ProductVariantId == variant.Id, cancellationToken);

            if (stock == null)
            {
                stock = new Stock
                {
                    BranchId = branchId,
                    ProductVariantId = variant.Id,
                    CurrentStock = 0,
                    Branch = branch,
                    ProductVariant = variant,
                };
                await _dbContext.Stocks.AddAsync(stock, cancellationToken);
            }

            cache[key] = stock;
            return stock;
        }
    }
}
