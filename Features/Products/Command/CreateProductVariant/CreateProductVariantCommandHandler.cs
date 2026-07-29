using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Products.Shared.Dtos;
using Inventory_Management_System.Features.Products.Shared.Repository;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.CreateProductVariant
{
    public class CreateProductVariantCommandHandler(
        IProductRepository _productRepository,
        IBaseRepository<ProductVariant> _variantRepository,
        ILogger<CreateProductVariantCommandHandler> _logger
    ) : IRequestHandler<CreateProductVariantCommand, Result>
    {
        public async Task<Result> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
            if (product is null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Product with id {request.ProductId} was not found." };

            var existing = await _variantRepository.GetAsync(v => v.SKU == request.SKU, asNoTracking: true, cancellationToken);
            if (existing is not null)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"A variant with SKU '{request.SKU}' already exists." };

            try
            {
                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    SKU = request.SKU,
                    Barcode = request.Barcode,
                    SellingPrice = request.SellingPrice,
                    IsSerialized = request.IsSerialized,
                    AttributesJson = string.IsNullOrWhiteSpace(request.AttributesJson) ? "{}" : request.AttributesJson,
                    Product = product,
                };

                await _variantRepository.AddAsync(variant, cancellationToken);
                await _variantRepository.SaveChangesAsync(cancellationToken);

                var response = new ProductVariantResponse(
                    variant.Id, variant.ProductId, product.ProductName, variant.SKU, variant.Barcode,
                    variant.SellingPrice, variant.IsSerialized, variant.AttributesJson, variant.CreatedAt);

                return new Result { IsSuccess = true, StatusCode = 201, Status = "Success", Message = "Variant created successfully", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product variant");
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while creating the variant." };
            }
        }
    }
}
