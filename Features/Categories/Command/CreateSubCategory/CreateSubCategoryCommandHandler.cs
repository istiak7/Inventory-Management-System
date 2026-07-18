using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.CreateSubCategory
{
    public class CreateSubCategoryCommandHandler(
        IProductCategoryRepository _categoryRepository,
        IProductSubCategoryRepository _subCategoryRepository,
        ILogger<CreateSubCategoryCommandHandler> _logger
    ) : IRequestHandler<CreateSubCategoryCommand, Result>
    {
        public async Task<Result> Handle(CreateSubCategoryCommand request, CancellationToken cancellationToken)
        {
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

            var existing = await _subCategoryRepository.GetAsync(s => s.Code == request.Code);
            if (existing is not null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"A sub-category with code '{request.Code}' already exists."
                };
            }

            try
            {
                var subCategory = new ProductSubCategories
                {
                    SubCategoryName = request.SubCategoryName,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl,
                    Code = request.Code,
                    ProductCategoryId = request.ProductCategoryId,
                    ProductCategories = category,
                    CreatedAt = DateTime.UtcNow
                };

                await _subCategoryRepository.AddAsync(subCategory, cancellationToken);
                await _subCategoryRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Sub-category created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sub-category");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the sub-category."
                };
            }
        }
    }
}
