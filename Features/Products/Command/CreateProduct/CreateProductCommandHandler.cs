using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.CreateProduct
{
    public class CreateProductCommandHandler(
        IProductRepository _productRepository,
        IProductSubCategoryRepository _subCategoryRepository,
        IBrandRepository _brandRepository,
        ILogger<CreateProductCommandHandler> _logger
    ) : IRequestHandler<CreateProductCommand, Result>
    {
        public async Task<Result> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.GetByIdAsync(request.ProductSubCategoryId, cancellationToken);
            if (subCategory is null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = $"Sub-category with id {request.ProductSubCategoryId} was not found."
                };
            }

            var brand = await _brandRepository.GetByIdAsync(request.BrandId, cancellationToken);
            if (brand is null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = $"Brand with id {request.BrandId} was not found."
                };
            }

            var existing = await _productRepository.GetAsync(p => p.SKU == request.ProductCode);
            if (existing is not null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = $"A product with code '{request.ProductCode}' already exists."
                };
            }

            try
            {
                var product = new Product
                {
                    ProductName = request.ProductName,
                    ProductDescription = request.ProductDescription,
                    ProductImageUrl = request.ProductImageUrl,
                    SKU = request.ProductCode,
                    ProductPrice = request.ProductPrice,
                    ProductSubCategoryId = request.ProductSubCategoryId,
                    ProductSubCategories = subCategory,
                    BrandId = request.BrandId,
                    Brand = brand,
                    CreatedAt = DateTime.UtcNow
                };

                await _productRepository.AddAsync(product, cancellationToken);
                await _productRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Product created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the product."
                };
            }
        }
    }
}
