using Inventory_Management_System.Features.Categories.Shared.Dtos;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQueryHandler(
        IProductCategoryRepository _categoryRepository,
        ILogger<GetCategoryByIdQueryHandler> _logger
    ) : IRequestHandler<GetCategoryByIdQuery, Result>
    {
        public async Task<Result> Handle(
            GetCategoryByIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(
                    request.Id,
                    cancellationToken);

                if (category is null)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Not Found",
                        Message = $"Category with id {request.Id} was not found."
                    };
                }

                var response = new CategoryResponse(
                    category.Id,
                    category.CategoryName,
                    category.Description,
                    category.ImageUrl,
                    category.Code,
                    category.CreatedAt);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Category retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving category with id {Id}",
                    request.Id);

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the category."
                };
            }
        }
    }
}
