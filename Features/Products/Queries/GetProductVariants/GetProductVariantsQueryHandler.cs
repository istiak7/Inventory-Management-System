using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Products.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Products.Queries.GetProductVariants
{
    public class GetProductVariantsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetProductVariantsQueryHandler> _logger
    ) : IRequestHandler<GetProductVariantsQuery, Result>
    {
        public async Task<Result> Handle(GetProductVariantsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _dbContext.ProductVariants.AsNoTracking();

                if (request.ProductId is int productId)
                    query = query.Where(v => v.ProductId == productId);

                var variants = await query
                    .OrderBy(v => v.ProductId).ThenBy(v => v.Id)
                    .Select(v => new ProductVariantResponse(
                        v.Id,
                        v.ProductId,
                        v.Product.ProductName,
                        v.SKU,
                        v.Barcode,
                        v.SellingPrice,
                        v.IsSerialized,
                        v.AttributesJson,
                        v.CreatedAt))
                    .ToListAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Product variants retrieved successfully", Data = variants };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product variants");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving product variants." };
            }
        }
    }
}
