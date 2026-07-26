using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.CreateProduct
{
    public class CreateProductCommandHandler(
        IProductRepository _productRepository,
        IBaseRepository<ProductVariant> _variantRepository,
        IProductSubCategoryRepository _subCategoryRepository,
        IBrandRepository _brandRepository,
        ILogger<CreateProductCommandHandler> _logger
    ) : IRequestHandler<CreateProductCommand, Result>
    {
        public async Task<Result> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var subCategory = await _subCategoryRepository.GetByIdAsync(request.ProductSubCategoryId, cancellationToken);
            if (subCategory is null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Sub-category with id {request.ProductSubCategoryId} was not found." };

            var brand = await _brandRepository.GetByIdAsync(request.BrandId, cancellationToken);
            if (brand is null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Brand with id {request.BrandId} was not found." };

            // SKU is the globally-unique variant business key.
            var existing = await _variantRepository.GetAsync(v => v.SKU == request.SKU, asNoTracking: true, cancellationToken);
            if (existing is not null)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"A variant with SKU '{request.SKU}' already exists." };

            try
            {
                // Product and its first variant are created together, atomically (one SaveChanges).
                var product = new Product
                {
                    ProductName = request.ProductName,
                    ProductDescription = request.ProductDescription,
                    ProductImageUrl = request.ProductImageUrl,
                    ProductSubCategoryId = request.ProductSubCategoryId,
                    ProductSubCategories = subCategory,
                    BrandId = request.BrandId,
                    Brand = brand,
                    CreatedAt = DateTime.UtcNow
                };

                var variant = new ProductVariant
                {
                    SKU = request.SKU,
                    Barcode = request.Barcode,
                    SellingPrice = request.SellingPrice,
                    IsSerialized = request.IsSerialized,
                    AttributesJson = string.IsNullOrWhiteSpace(request.AttributesJson) ? "{}" : request.AttributesJson,
                    Product = product,
                };
                product.ProductVariants.Add(variant);

                await _productRepository.AddAsync(product, cancellationToken);
                await _productRepository.SaveChangesAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 201, Status = "Success", Message = "Product created successfully", Data = new { product.Id, VariantId = variant.Id } };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while creating the product." };
            }
        }
    }
}
