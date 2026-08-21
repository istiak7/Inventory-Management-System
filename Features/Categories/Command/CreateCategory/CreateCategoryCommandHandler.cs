using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.CreateCategory
{
    public class CreateCategoryCommandHandler(
        IProductCategoryRepository _categoryRepository,
        ILogger<CreateCategoryCommandHandler> _logger
    ) : IRequestHandler<CreateCategoryCommand, Result>
    {
        public async Task<Result> Handle(
            CreateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var existing = await _categoryRepository.GetAsync(c => c.Code == request.Code);
            if (existing is not null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"A category with code '{request.Code}' already exists."
                };
            }

            try
            {
                var category = new ProductCategories
                {
                    CategoryName = request.CategoryName,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl,
                    Code = request.Code,
                    CreatedAt = DateTime.UtcNow
                };

                await _categoryRepository.AddAsync(category, cancellationToken);
                await _categoryRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Category created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating category");

                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the category."
                };
            }
        }
    }
}
