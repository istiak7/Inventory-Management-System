using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace Inventory_Management_System.Features.Transfers.Command.CreateStockTransfer
{
    public class CreateStockTransferHandler(
        AppDbContext _dbContext,
        ILogger<CreateStockTransferHandler> _logger
    ) : IRequestHandler<CreateStockTransferCommand, Result>
    {
        public async Task<Result> Handle(CreateStockTransferCommand request, CancellationToken cancellationToken)
        {
            if (request.SourceBranchId == request.DestinationBranchId)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Source and destination must be different branches." };

            // Each product once: two lines of 6 must not both pass a stock check of 10.
            if (request.Items.GroupBy(i => i.ProductVariantId).Any(g => g.Count() > 1))
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "Each product can appear only once in a transfer. Put the total quantity on one line." };

            var sourceBranch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.SourceBranchId, cancellationToken);
            if (sourceBranch == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Source branch not found." };

            var destinationBranch = await _dbContext.Branches.FirstOrDefaultAsync(b => b.Id == request.DestinationBranchId, cancellationToken);
            if (destinationBranch == null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Destination branch not found." };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;

                var transfer = new StockTransfer
                {
                    Reference = await GenerateReferenceAsync(now, cancellationToken),
                    SourceBranchId = request.SourceBranchId,
                    DestinationBranchId = request.DestinationBranchId,
                    Status = request.Dispatch ? TransferStatus.Pending : TransferStatus.Draft,
                    Notes = request.Notes,
                    SubmittedAt = request.Dispatch ? now : null,
                    SourceBranch = sourceBranch,
                    DestinationBranch = destinationBranch,
                };

                var claimedSerials = new HashSet<string>();
                var built = new List<(StockTransferDetails Line, ProductVariant Variant, List<string> Serials)>();

                foreach (var item in request.Items)
                {
                    var variant = await _dbContext.ProductVariants
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, cancellationToken);
                    if (variant == null)
                        return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"Product variant with id {item.ProductVariantId} not found." };

                    List<string> serials = [];
                    int quantity;

                    if (variant.IsSerialized)
                    {
                        serials = (item.SerialNumbers ?? [])
                            .Select(s => s?.Trim() ?? string.Empty)
                            .Where(s => s.Length > 0)
                            .ToList();

                        if (serials.Count == 0)
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Serial numbers are required for '{variant.Product.ProductName}' (SKU {variant.SKU})." };

                        var dupes = new List<string>();
                        foreach (var s in serials)
                        {
                            bool isNewSerial = claimedSerials.Add(s);
                            if (!isNewSerial)
                            {
                                dupes.Add(s);
                            }
                        }
                        if (dupes.Count > 0)
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Duplicate serial numbers in this transfer: {string.Join(", ", dupes)}." };

                        var availableCount = await _dbContext.ProductSerials.IgnoreQueryFilters().CountAsync(s =>
                            serials.Contains(s.SerialNumber) &&
                            s.ProductVariantId == variant.Id &&
                            s.BranchId == request.SourceBranchId &&
                            s.Status == SerialStatus.InStock, cancellationToken);
                        if (availableCount != serials.Count)
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"One or more serial numbers for '{variant.Product.ProductName}' are not currently in stock at the source branch." };

                        quantity = serials.Count;
                    }
                    else
                    {
                        quantity = item.Quantity ?? 0;
                        if (quantity <= 0)
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"A quantity greater than 0 is required for '{variant.Product.ProductName}' (SKU {variant.SKU})." };

                        var stock = await _dbContext.Stocks.IgnoreQueryFilters().FirstOrDefaultAsync(s =>
                            s.BranchId == request.SourceBranchId && s.ProductVariantId == variant.Id, cancellationToken);
                        if (stock == null || stock.CurrentStock < quantity)
                            return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}) at the source branch: {stock?.CurrentStock ?? 0} on hand, {quantity} requested." };
                    }

                    var line = new StockTransferDetails
                    {
                        ProductVariantId = variant.Id,
                        Quantity = quantity,
                        RequestedSerialNumbersJson = JsonSerializer.Serialize(serials),
                        StockTransfer = transfer,
                        ProductVariant = variant,
                    };
                    transfer.StockTransferDetails.Add(line);
                    built.Add((line, variant, serials));
                }

                await _dbContext.StockTransfers.AddAsync(transfer, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var lineResponses = built.Select(b => new StockTransferLineResponse(
                    b.Line.Id, b.Variant.Id, b.Variant.SKU, b.Variant.Product.ProductName,
                    b.Variant.IsSerialized, b.Line.Quantity, b.Serials)).ToList();

                var response = new StockTransferResponse(
                    transfer.Id, transfer.Reference, transfer.SourceBranchId, sourceBranch.Name,
                    transfer.DestinationBranchId, destinationBranch.Name, transfer.Status.ToString(),
                    transfer.Notes, transfer.CreatedAt, transfer.SubmittedAt, transfer.ApprovedAt, transfer.RejectedAt,
                    lineResponses.Count, lineResponses.Sum(l => l.Quantity), lineResponses);

                return new Result { IsSuccess = true, StatusCode = 201, Status = "Success", Message = "Stock transfer created successfully", Data = response };
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Stock transfer creation lost a uniqueness race on Reference");
                return new Result { IsSuccess = false, StatusCode = 409, Status = "Error", Message = "This transfer clashed with another one saved at the same moment. Please try again." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error creating stock transfer");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while creating the stock transfer." };
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

        private Task<string> GenerateReferenceAsync(DateTime date, CancellationToken cancellationToken) =>
            DocumentNumbers.NextAsync(
                _dbContext,
                _dbContext.StockTransfers.IgnoreQueryFilters().Select(t => t.Reference),
                $"TR-{BusinessClock.ToLocal(date).Year}-",
                cancellationToken);
    }
}
