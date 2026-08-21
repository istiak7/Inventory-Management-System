using Inventory_Management_System.Features.Categories.Shared.Dtos;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetSubCategoryById
{
    public class GetSubCategoryByIdQueryHandler(
        IProductSubCategoryRepository _subCategoryRepository,
        ILogger<GetSubCategoryByIdQueryHandler> _logger
    ) : IRequestHandler<GetSubCategoryByIdQuery, Result>
    {
        public async Task<Result> Handle(
            GetSubCategoryByIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var subCategory = await _subCategoryRepository.GetByIdAsync(
                    request.Id,
                    cancellationToken);

                if (subCategory is null)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Not Found",
                        Message = $"Sub-category with id {request.Id} was not found."
                    };
                }

                var response = new SubCategoryResponse(
                    subCategory.Id,
                    subCategory.SubCategoryName,
                    subCategory.Description,
                    subCategory.ImageUrl,
                    subCategory.Code,
                    subCategory.ProductCategoryId,
                    subCategory.CreatedAt);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Sub-category retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving sub-category with id {Id}",
                    request.Id);

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the sub-category."
                };
            }
        }
    }
}
