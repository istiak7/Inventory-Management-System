using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Products.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetProductByIdQueryHandler> _logger
    ) : IRequestHandler<GetProductByIdQuery, Result>
    {
        public async Task<Result> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _dbContext.Products
                    .AsNoTracking()
                    .Where(p => p.Id == request.Id)
                    .Select(p => new ProductResponse(
                        p.Id,
                        p.ProductName,
                        p.ProductDescription,
                        p.ProductImageUrl,
                        p.ProductSubCategoryId,
                        p.BrandId,
                        p.CreatedAt,
                        p.ProductVariants.OrderBy(v => v.Id).Select(v => v.Id).FirstOrDefault(),
                        p.ProductVariants.OrderBy(v => v.Id).Select(v => v.SKU).FirstOrDefault() ?? string.Empty,
                        p.ProductVariants.OrderBy(v => v.Id).Select(v => v.SellingPrice).FirstOrDefault(),
                        p.ProductVariants.OrderBy(v => v.Id).Select(v => v.IsSerialized).FirstOrDefault()))
                    .FirstOrDefaultAsync(cancellationToken);

                if (response is null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Product with id {request.Id} was not found." };

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Product retrieved successfully", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with id {Id}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving the product." };
            }
        }
    }
}
