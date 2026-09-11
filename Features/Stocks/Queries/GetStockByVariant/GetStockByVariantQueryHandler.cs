using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Stocks.Shared;
using Inventory_Management_System.Features.Stocks.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariant
{
    public class GetStockByVariantQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetStockByVariantQueryHandler> _logger
    ) : IRequestHandler<GetStockByVariantQuery, Result>
    {
        public async Task<Result> Handle(GetStockByVariantQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var stock = await _dbContext.Stocks
                    .AsNoTracking()
                    .Where(s => s.ProductVariantId == request.ProductVariantId)
                    .OrderBy(s => s.BranchId)
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
                    .ToListAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock retrieved successfully", Data = stock };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock for variant {VariantId}", request.ProductVariantId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving stock." };
            }
        }
    }
}
