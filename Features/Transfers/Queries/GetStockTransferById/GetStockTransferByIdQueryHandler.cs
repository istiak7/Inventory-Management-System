using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory_Management_System.Features.Transfers.Queries.GetStockTransferById
{
    public class GetStockTransferByIdQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetStockTransferByIdQueryHandler> _logger
    ) : IRequestHandler<GetStockTransferByIdQuery, Result>
    {
        public async Task<Result> Handle(GetStockTransferByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var transfer = await _dbContext.StockTransfers
                    .AsNoTracking()
                    .Where(t => t.Id == request.Id)
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
                    .FirstOrDefaultAsync(cancellationToken);

                if (transfer == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Stock transfer not found." };

                var lineResponses = transfer.Lines.Select(l => new StockTransferLineResponse(
                    l.Id, l.ProductVariantId, l.Sku, l.ProductName, l.IsSerialized, l.Quantity,
                    JsonSerializer.Deserialize<List<string>>(l.RequestedSerialNumbersJson) ?? [])).ToList();

                var response = new StockTransferResponse(
                    transfer.Id, transfer.Reference, transfer.SourceBranchId, transfer.SourceBranchName,
                    transfer.DestinationBranchId, transfer.DestinationBranchName, transfer.Status.ToString(),
                    transfer.Notes, transfer.CreatedAt, transfer.SubmittedAt, transfer.ApprovedAt, transfer.RejectedAt,
                    lineResponses.Count, lineResponses.Sum(l => l.Quantity), lineResponses);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock transfer retrieved successfully", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock transfer {Id}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving the stock transfer." };
            }
        }
    }
}
