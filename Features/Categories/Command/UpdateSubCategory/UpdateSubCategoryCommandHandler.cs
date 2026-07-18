using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.UpdateSubCategory
{
    public class UpdateSubCategoryCommandHandler(
        IProductSubCategoryRepository _subCategoryRepository,
        IProductCategoryRepository _categoryRepository,
        ILogger<UpdateSubCategoryCommandHandler> _logger
    ) : IRequestHandler<UpdateSubCategoryCommand, Result>
    {
        public async Task<Result> Handle(UpdateSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.GetByIdAsync(request.Id, cancellationToken);

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

            var category = await _categoryRepository.GetByIdAsync(request.ProductCategoryId, cancellationToken);

            if (category is null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = $"Category with id {request.ProductCategoryId} was not found."
                };
            }

            try
            {
                subCategory.SubCategoryName = request.SubCategoryName;
                subCategory.Description = request.Description;
                subCategory.ImageUrl = request.ImageUrl;
                subCategory.Code = request.Code;
                subCategory.ProductCategoryId = request.ProductCategoryId;
                subCategory.UpDatedAt = DateTime.UtcNow;

                await _subCategoryRepository.UpdateAsync(subCategory, cancellationToken);
                await _subCategoryRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Sub-category updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sub-category with id {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the sub-category."
                };
            }
        }
    }
}
