using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory_Management_System.Features.Transfers.Shared
{
    public sealed record BuiltTransferLine(StockTransferDetails Line, ProductVariant Variant, List<string> Serials);

    // Checks the requested lines against the source branch and builds them onto the transfer.
    // Shared by create and update so a transfer is held to the same rules either way. Nothing
    // is reserved: stock only moves when the transfer is approved, which checks again.
    public static class TransferLineBuilder
    {
        public static Result? CheckHeader(int sourceBranchId, int destinationBranchId, IReadOnlyCollection<TransferItemRequest> items)
        {
            if (sourceBranchId == destinationBranchId)
                return Error(400, "Source and destination must be different branches.");

            // Each product once: two lines of 6 must not both pass a stock check of 10.
            if (items.GroupBy(i => i.ProductVariantId).Any(g => g.Count() > 1))
                return Error(400, "Each product can appear only once in a transfer. Put the total quantity on one line.");

            return null;
        }

        public static async Task<(List<BuiltTransferLine>? Lines, Result? Error)> BuildAsync(
            AppDbContext db,
            StockTransfer transfer,
            int sourceBranchId,
            IEnumerable<TransferItemRequest> items,
            CancellationToken cancellationToken)
        {
            var claimedSerials = new HashSet<string>();
            var built = new List<BuiltTransferLine>();

            foreach (var item in items)
            {
                var variant = await db.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId, cancellationToken);
                if (variant == null)
                    return (null, Error(404, $"Product variant with id {item.ProductVariantId} not found."));

                List<string> serials = [];
                int quantity;

                if (variant.IsSerialized)
                {
                    serials = (item.SerialNumbers ?? [])
                        .Select(s => s?.Trim() ?? string.Empty)
                        .Where(s => s.Length > 0)
                        .ToList();

                    if (serials.Count == 0)
                        return (null, Error(400, $"Serial numbers are required for '{variant.Product.ProductName}' (SKU {variant.SKU})."));

                    var dupes = serials.Where(s => !claimedSerials.Add(s)).ToList();
                    if (dupes.Count > 0)
                        return (null, Error(400, $"Duplicate serial numbers in this transfer: {string.Join(", ", dupes)}."));

                    var availableCount = await db.ProductSerials.IgnoreQueryFilters().CountAsync(s =>
                        serials.Contains(s.SerialNumber) &&
                        s.ProductVariantId == variant.Id &&
                        s.BranchId == sourceBranchId &&
                        s.Status == SerialStatus.InStock, cancellationToken);
                    if (availableCount != serials.Count)
                        return (null, Error(400, $"One or more serial numbers for '{variant.Product.ProductName}' are not currently in stock at the source branch."));

                    quantity = serials.Count;
                }
                else
                {
                    quantity = item.Quantity ?? 0;
                    if (quantity <= 0)
                        return (null, Error(400, $"A quantity greater than 0 is required for '{variant.Product.ProductName}' (SKU {variant.SKU})."));

                    var stock = await db.Stocks.IgnoreQueryFilters().FirstOrDefaultAsync(s =>
                        s.BranchId == sourceBranchId && s.ProductVariantId == variant.Id, cancellationToken);
                    if (stock == null || stock.CurrentStock < quantity)
                        return (null, Error(400, $"Insufficient stock for '{variant.Product.ProductName}' (SKU {variant.SKU}) at the source branch: {stock?.CurrentStock ?? 0} on hand, {quantity} requested."));
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
                built.Add(new BuiltTransferLine(line, variant, serials));
            }

            return (built, null);
        }

        public static StockTransferResponse ToResponse(StockTransfer transfer, string sourceName, string destinationName, IEnumerable<BuiltTransferLine> built)
        {
            var lineResponses = built.Select(b => new StockTransferLineResponse(
                b.Line.Id, b.Variant.Id, b.Variant.SKU, b.Variant.Product.ProductName,
                b.Variant.IsSerialized, b.Line.Quantity, b.Serials)).ToList();

            return new StockTransferResponse(
                transfer.Id, transfer.Reference, transfer.SourceBranchId, sourceName,
                transfer.DestinationBranchId, destinationName, transfer.Status.ToString(),
                transfer.Notes, transfer.CreatedAt, transfer.SubmittedAt, transfer.ApprovedAt, transfer.RejectedAt,
                lineResponses.Count, lineResponses.Sum(l => l.Quantity), lineResponses);
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
