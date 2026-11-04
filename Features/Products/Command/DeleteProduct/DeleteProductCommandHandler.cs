using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Products.Command.DeleteProduct
{
    // Deletes a product and its variants, only while none of them has been bought, sold, moved or stocked.
    public class DeleteProductCommandHandler(
        AppDbContext _db,
        ILogger<DeleteProductCommandHandler> _logger
    ) : IRequestHandler<DeleteProductCommand, Result>
    {
        public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.Products.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Product not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            var ids = await _db.ProductVariants.IgnoreQueryFilters()
                .Where(v => v.ProductId == id).Select(v => v.Id).ToListAsync(cancellationToken);
            if (await _db.SaleDetails.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This product cannot be deleted. It has been sold, so it is kept for the sales history.");
            if (await _db.SupplierPurchaseDetails.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This product cannot be deleted. It is on purchase orders, so it is kept for the purchase history.");
            if (await _db.StockTransferDetails.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This product cannot be deleted. It is on stock transfers, so it is kept for the transfer history.");
            if (await _db.InventoryTransactions.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This product cannot be deleted. It has stock movements, so it is kept for the stock history.");
            if (await _db.ProductSerials.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId), cancellationToken))
                return Fail(400, "This product cannot be deleted. It has serial numbers recorded, so it is kept.");
            if (await _db.Stocks.IgnoreQueryFilters().AnyAsync(x => ids.Contains(x.ProductVariantId) && x.CurrentStock != 0, cancellationToken))
                return Fail(400, "This product cannot be deleted. It still has stock in a warehouse.");

            try
            {
                // Empty stock rows (quantity 0) go with the variants.
                _db.Stocks.RemoveRange(_db.Stocks.IgnoreQueryFilters().Where(s => ids.Contains(s.ProductVariantId)));
                _db.ProductVariants.RemoveRange(_db.ProductVariants.IgnoreQueryFilters().Where(v => ids.Contains(v.Id)));
                _db.Products.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Product deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Product {Id}", id);
                return Fail(400, "Product is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Product {Id}", id);
                return Fail(500, "An error occurred while deleting the product.");
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
