using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory_Management_System.Features.Transfers.Queries.GetAllStockTransfers
{
    public class GetAllStockTransfersQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllStockTransfersQueryHandler> _logger
    ) : IRequestHandler<GetAllStockTransfersQuery, Result>
    {
        public async Task<Result> Handle(GetAllStockTransfersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.StockTransfers.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(request.Status) &&
                    Enum.TryParse<TransferStatus>(request.Status, true, out var statusFilter))
                    query = query.Where(t => t.Status == statusFilter);

                if (request.SourceBranchId is int sourceBranchId)
                    query = query.Where(t => t.SourceBranchId == sourceBranchId);

                if (request.DestinationBranchId is int destinationBranchId)
                    query = query.Where(t => t.DestinationBranchId == destinationBranchId);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(t =>
                        EF.Functions.ILike(t.Reference, term) ||
                        EF.Functions.ILike(t.SourceBranch.Name, term) ||
                        EF.Functions.ILike(t.DestinationBranch.Name, term) ||
                        t.StockTransferDetails.Any(d =>
                            EF.Functions.ILike(d.ProductVariant.SKU, term) ||
                            EF.Functions.ILike(d.ProductVariant.Product.ProductName, term)));
                }

                // Materialize with the enum and the raw serial JSON intact, then map to string
                // DTOs (and deserialize the JSON) in memory — EF cannot translate either.
                var paged = await query
                    .OrderByDescending(t => t.Id)
                    .Select(t => new
                    {
                        t.Id,
                        t.Reference,
                        t.SourceBranchId,
                        SourceBranchName = t.SourceBranch.Name,
                        t.DestinationBranchId,
                        DestinationBranchName = t.DestinationBranch.Name,
                        t.Status,
                        t.Notes,
                        t.CreatedAt,
                        t.SubmittedAt,
                        t.ApprovedAt,
                        t.RejectedAt,
                        Lines = t.StockTransferDetails.Select(d => new
                        {
                            d.Id,
                            d.ProductVariantId,
                            Sku = d.ProductVariant.SKU,
                            ProductName = d.ProductVariant.Product.ProductName,
                            d.ProductVariant.IsSerialized,
                            d.Quantity,
                            d.RequestedSerialNumbersJson
                        }).ToList()
                    })
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                var items = paged.Items.Select(t => new StockTransferResponse(
                    t.Id, t.Reference, t.SourceBranchId, t.SourceBranchName,
                    t.DestinationBranchId, t.DestinationBranchName, t.Status.ToString(),
                    t.Notes, t.CreatedAt, t.SubmittedAt, t.ApprovedAt, t.RejectedAt,
                    t.Lines.Count, t.Lines.Sum(l => l.Quantity),
                    t.Lines.Select(l => new StockTransferLineResponse(
                        l.Id, l.ProductVariantId, l.Sku, l.ProductName, l.IsSerialized, l.Quantity,
                        JsonSerializer.Deserialize<List<string>>(l.RequestedSerialNumbersJson) ?? [])).ToList()))
                    .ToList();

                var pagedResult = new PagedResult<StockTransferResponse>
                {
                    Items = items,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalCount = paged.TotalCount
                };

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock transfers retrieved successfully", Data = pagedResult };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock transfers");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving stock transfers." };
            }
        }
    }
}
