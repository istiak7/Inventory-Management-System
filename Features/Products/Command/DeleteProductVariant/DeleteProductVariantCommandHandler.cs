using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Products.Command.DeleteProductVariant
{
    // Deletes a variant only while it has never been bought, sold, moved or stocked.
    public class DeleteProductVariantCommandHandler(
        AppDbContext _db,
        ILogger<DeleteProductVariantCommandHandler> _logger
    ) : IRequestHandler<DeleteProductVariantCommand, Result>
    {
        public async Task<Result> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.ProductVariants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Product variant not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            var ids = new List<int> { id };
            if (await _db.SaleDetails.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This variant cannot be deleted. It has been sold, so it is kept for the sales history.");
            if (await _db.SupplierPurchaseDetails.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This variant cannot be deleted. It is on purchase orders, so it is kept for the purchase history.");
            if (await _db.StockTransferDetails.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This variant cannot be deleted. It is on stock transfers, so it is kept for the transfer history.");
            if (await _db.InventoryTransactions.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This variant cannot be deleted. It has stock movements, so it is kept for the stock history.");
            if (await _db.ProductSerials.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This variant cannot be deleted. It has serial numbers recorded, so it is kept.");
            if (await _db.Stocks.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId) && x.CurrentStock != 0, cancellationToken))
                return Fail(400, "This variant cannot be deleted. It still has stock in a warehouse.");

            try
            {
                _db.Stocks.RemoveRange(_db.Stocks.IgnoreQueryFilters().Where(s => s.ProductVariantId == id));
                _db.ProductVariants.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Product variant deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Product variant {Id}", id);
                return Fail(400, "Product variant is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Product variant {Id}", id);
                return Fail(500, "An error occurred while deleting the product variant.");
            }
        }

        private static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message
        };
    }
}
