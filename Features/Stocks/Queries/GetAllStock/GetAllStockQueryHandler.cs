using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Stocks.Shared;
using Inventory_Management_System.Features.Stocks.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Stocks.Queries.GetAllStock
{
    public class GetAllStockQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllStockQueryHandler> _logger
    ) : IRequestHandler<GetAllStockQuery, Result>
    {
        public async Task<Result> Handle(GetAllStockQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.Stocks.AsNoTracking();

                if (request.BranchId is int branchId)
                    query = query.Where(s => s.BranchId == branchId);

                if (request.ProductVariantId is int variantId)
                    query = query.Where(s => s.ProductVariantId == variantId);

                if (request.LowStockThreshold is int threshold)
                    query = query.Where(s => s.CurrentStock <= threshold);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(s =>
                        EF.Functions.ILike(s.ProductVariant.SKU, term) ||
                        EF.Functions.ILike(s.ProductVariant.Product.ProductName, term));
                }

                var pagedResult = await query
                    .OrderBy(s => s.BranchId).ThenBy(s => s.ProductVariantId)
                    .Select(s => new StockResponse(
                        s.Id,
                        s.BranchId,
                        s.Branch.Name,
                        s.ProductVariantId,
                        s.ProductVariant.SKU,
                        s.ProductVariant.Product.ProductName,
                        s.ProductVariant.IsSerialized,
                        s.CurrentStock,
                        s.CurrentStock > 0 ? StockStatuses.InStock : StockStatuses.OutOfStock))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock retrieved successfully", Data = pagedResult };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving stock." };
            }
        }
    }
}
