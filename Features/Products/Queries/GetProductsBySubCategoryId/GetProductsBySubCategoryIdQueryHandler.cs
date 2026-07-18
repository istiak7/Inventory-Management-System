using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductsBySubCategoryId
{
    public class GetProductsBySubCategoryIdQueryHandler(
        IProductRepository _productRepository,
        IProductSubCategoryRepository _subCategoryRepository,
        ILogger<GetProductsBySubCategoryIdQueryHandler> _logger
    ) : IRequestHandler<GetProductsBySubCategoryIdQuery, Result>
    {
        public async Task<Result> Handle(GetProductsBySubCategoryIdQuery request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.GetByIdAsync(request.SubCategoryId, cancellationToken);

            if (subCategory is null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = $"Sub-category with id {request.SubCategoryId} was not found."
                };
            }

            try
            {
                var pagedResult = await _productRepository.GetBySubCategoryIdPagedAsync(
                    request.SubCategoryId,
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
                _logger.LogError(ex, "Error retrieving products for sub-category id {SubCategoryId}", request.SubCategoryId);
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
