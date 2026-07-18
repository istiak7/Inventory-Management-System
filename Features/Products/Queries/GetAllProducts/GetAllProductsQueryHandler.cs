using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsQueryHandler(
        IProductRepository _productRepository,
        ILogger<GetAllProductsQueryHandler> _logger
    ) : IRequestHandler<GetAllProductsQuery, Result>
    {
        public async Task<Result> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagedResult = await _productRepository.GetAllPagedAsync(
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
                _logger.LogError(ex, "Error retrieving products");
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
