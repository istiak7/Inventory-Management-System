using Inventory_Management_System.Features.Brands.Shared.Repository;
using Inventory_Management_System.Features.Categories.Shared.Repository;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Database;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Products.Command.UpdateProduct
{
    public class UpdateProductCommandHandler(
        IProductRepository _productRepository,
        IProductSubCategoryRepository _subCategoryRepository,
        IBrandRepository _brandRepository,
        AppDbContext _db,
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

            // A product may keep its current unit even after it was made inactive; picking a
            // different one needs an Active unit.
            var unit = await _db.UnitOfMeasures.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UnitOfMeasureId, cancellationToken);
            if (unit is null)
            {
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Unit of measurement with id {request.UnitOfMeasureId} was not found." };
            }
            if (unit.IsActive != (int)EntityStatus.Active && product.UnitOfMeasureId != unit.Id)
            {
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Unit of measurement '{unit.Name}' is inactive and cannot be chosen." };
            }

            try
            {
                product.ProductName = request.ProductName;
                // Fields the form no longer sends (null) keep what is stored.
                if (request.ProductDescription is not null) product.ProductDescription = request.ProductDescription;
                if (request.ProductImageUrl is not null) product.ProductImageUrl = request.ProductImageUrl;
                if (request.IsReturnable is bool isReturnable)
                    product.IsReturnable = isReturnable;
                if (request.Warranty is not null)
                    product.Warranty = string.IsNullOrWhiteSpace(request.Warranty) ? null : request.Warranty.Trim();
                product.ProductSubCategoryId = request.ProductSubCategoryId;
                product.BrandId = request.BrandId;
                product.UnitOfMeasureId = unit.Id;
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
