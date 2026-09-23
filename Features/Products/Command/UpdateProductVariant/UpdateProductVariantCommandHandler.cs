using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.UpdateProductVariant
{
    public class UpdateProductVariantCommandHandler(
        IBaseRepository<ProductVariant> _variantRepository,
        AppDbContext _dbContext,
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

            // Switching serial tracking on or off while units exist would break them: serialized
            // stock without serials could never be sold, and serials would be left behind.
            if (variant.IsSerialized != request.IsSerialized)
            {
                var inUse =
                    await _dbContext.Stocks.IgnoreQueryFilters().AnyAsync(s => s.ProductVariantId == variant.Id && s.CurrentStock != 0, cancellationToken)
                    || await _dbContext.ProductSerials.IgnoreQueryFilters().AnyAsync(s => s.ProductVariantId == variant.Id, cancellationToken)
                    || await _dbContext.SupplierPurchaseDetails.AnyAsync(d => d.ProductVariantId == variant.Id
                            && (d.Status == LineStatus.Pending || d.Status == LineStatus.PartiallyReceived), cancellationToken)
                    || await _dbContext.StockTransfers.IgnoreQueryFilters().AnyAsync(t =>
                            (t.Status == TransferStatus.Draft || t.Status == TransferStatus.Pending)
                            && t.StockTransferDetails.Any(d => d.ProductVariantId == variant.Id), cancellationToken);

                if (inUse)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = "Serial tracking can only be changed while this variant has no stock, no serial numbers and no open purchase orders or transfers."
                    };
            }

            try
            {
                variant.SKU = request.SKU;
                variant.Barcode = request.Barcode;
                variant.SellingPrice = request.SellingPrice;
                variant.IsSerialized = request.IsSerialized;
                variant.AttributesJson = string.IsNullOrWhiteSpace(request.AttributesJson) ? "{}" : request.AttributesJson;

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
