using Inventory_Management_System.Features.Products.Shared.Dtos;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductById
{
    public class GetProductByIdQueryHandler(
        IProductRepository _productRepository,
        ILogger<GetProductByIdQueryHandler> _logger
    ) : IRequestHandler<GetProductByIdQuery, Result>
    {
        public async Task<Result> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

                if (product is null)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Not Found",
                        Message = $"Product with id {request.Id} was not found."
                    };
                }

                var response = new ProductResponse(
                    product.Id,
                    product.ProductName,
                    product.ProductDescription,
                    product.ProductImageUrl,
                    product.ProductCode,
                    product.ProductPrice,
                    product.ProductSubCategoryId,
                    product.BrandId,
                    product.CreatedAt);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Product retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with id {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the product."
                };
            }
        }
    }
}
