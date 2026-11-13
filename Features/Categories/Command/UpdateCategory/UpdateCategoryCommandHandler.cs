using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.UpdateCategory
{
    public class UpdateCategoryCommandHandler(
        IProductCategoryRepository _categoryRepository,
        ILogger<UpdateCategoryCommandHandler> _logger
    ) : IRequestHandler<UpdateCategoryCommand, Result>
    {
        public async Task<Result> Handle(
            UpdateCategoryCommand request,
            CancellationToken cancellationToken)
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

            try
            {
                category.CategoryName = request.CategoryName;
                // Fields the form no longer sends (null) keep what is stored.
                if (request.Description is not null) category.Description = request.Description;
                if (request.ImageUrl is not null) category.ImageUrl = request.ImageUrl;
                if (!string.IsNullOrWhiteSpace(request.Code)) category.Code = request.Code.Trim();
                category.UpDatedAt = DateTime.UtcNow;

                await _categoryRepository.UpdateAsync(category, cancellationToken);
                await _categoryRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Category updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating category with id {Id}",
                    request.Id);

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the category."
                };
            }
        }
    }
}
