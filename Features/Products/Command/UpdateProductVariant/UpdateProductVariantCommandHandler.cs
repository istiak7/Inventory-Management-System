using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.UpdateProductVariant
{
    public class UpdateProductVariantCommandHandler(
        IBaseRepository<ProductVariant> _variantRepository,
        ILogger<UpdateProductVariantCommandHandler> _logger
    ) : IRequestHandler<UpdateProductVariantCommand, Result>
    {
        public async Task<Result> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
        {
            var variant = await _variantRepository.GetByIdAsync(request.Id, cancellationToken);
            if (variant is null)
                return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Variant with id {request.Id} was not found." };

            var clash = await _variantRepository.GetAsync(v => v.SKU == request.SKU && v.Id != request.Id, asNoTracking: true, cancellationToken);
            if (clash is not null)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"A variant with SKU '{request.SKU}' already exists." };

            try
            {
                variant.SKU = request.SKU;
                variant.Barcode = request.Barcode;
                variant.SellingPrice = request.SellingPrice;
                variant.IsSerialized = request.IsSerialized;
                variant.AttributesJson = string.IsNullOrWhiteSpace(request.AttributesJson) ? "{}" : request.AttributesJson;
                variant.UpDatedAt = DateTime.UtcNow;

                await _variantRepository.UpdateAsync(variant, cancellationToken);
                await _variantRepository.SaveChangesAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Variant updated successfully" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product variant {Id}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while updating the variant." };
            }
        }
    }
}
