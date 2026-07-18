using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesQueryHandler(
        IProductCategoryRepository _categoryRepository,
        ILogger<GetAllCategoriesQueryHandler> _logger
    ) : IRequestHandler<GetAllCategoriesQuery, Result>
    {
        public async Task<Result> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagedResult = await _categoryRepository.GetAllPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Categories retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving categories."
                };
            }
        }
    }
}
