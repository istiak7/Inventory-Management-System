using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetProductsByBrandId
{
    public class GetProductsByBrandIdQueryHandler(
        IBrandRepository _brandRepository,
        IProductRepository _productRepository,
        ILogger<GetProductsByBrandIdQueryHandler> _logger
    ) : IRequestHandler<GetProductsByBrandIdQuery, Result>
    {
        public async Task<Result> Handle(GetProductsByBrandIdQuery request, CancellationToken cancellationToken)
        {
            var brand = await _brandRepository.GetByIdAsync(request.BrandId, cancellationToken);

            if (brand is null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = $"Brand with id {request.BrandId} was not found."
                };
            }

            try
            {
                var pagedResult = await _productRepository.GetByBrandIdPagedAsync(
                    request.BrandId,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Products retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for brand id {BrandId}", request.BrandId);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving products."
                };
            }
        }
    }
}
