using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetSubCategoryByCategoryId
{
    public class GetSubCategoryByCategoryIdQueryHandler(
        IProductSubCategoryRepository _subCategoryRepository,
        IProductCategoryRepository _categoryRepository,
        ILogger<GetSubCategoryByCategoryIdQueryHandler> _logger
    ) : IRequestHandler<GetSubCategoryByCategoryIdQuery, Result>
    {
        public async Task<Result> Handle(
            GetSubCategoryByCategoryIdQuery request,
            CancellationToken cancellationToken)
        {
            var category = await _categoryRepository.GetByIdAsync(
                request.CategoryId,
                cancellationToken);

            if (category is null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = $"Category with id {request.CategoryId} was not found."
                };
            }

            try
            {
                var pagedResult = await _subCategoryRepository.GetByCategoryIdPagedAsync(
                    request.CategoryId,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Sub-categories retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving sub-categories for category id {CategoryId}",
                    request.CategoryId);

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving sub-categories."
                };
            }
        }
    }
}
