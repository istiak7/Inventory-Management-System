using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetAllSubCategories
{
    public class GetAllSubCategoriesQueryHandler(
        IProductSubCategoryRepository _subCategoryRepository,
        ILogger<GetAllSubCategoriesQueryHandler> _logger
    ) : IRequestHandler<GetAllSubCategoriesQuery, Result>
    {
        public async Task<Result> Handle(
            GetAllSubCategoriesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var pagedResult = await _subCategoryRepository.GetAllPagedAsync(
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
                    "Error retrieving sub-categories");

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
