using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.UpdateProduct
{
    public class UpdateProductCommandHandler(
        IProductRepository _productRepository,
        IProductSubCategoryRepository _subCategoryRepository,
        IBrandRepository _brandRepository,
        ILogger<UpdateProductCommandHandler> _logger
    ) : IRequestHandler<UpdateProductCommand, Result>
    {
        public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
            if (product is null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = $"Product with id {request.Id} was not found."
                };
            }

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

            try
            {
                product.ProductName = request.ProductName;
                product.ProductDescription = request.ProductDescription;
                product.ProductImageUrl = request.ProductImageUrl;
                product.SKU = request.ProductCode;
                product.ProductPrice = request.ProductPrice;
                product.ProductSubCategoryId = request.ProductSubCategoryId;
                product.BrandId = request.BrandId;
                product.UpDatedAt = DateTime.UtcNow;

                await _productRepository.UpdateAsync(product, cancellationToken);
                await _productRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Product updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with id {Id}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the product."
                };
            }
        }
    }
}
