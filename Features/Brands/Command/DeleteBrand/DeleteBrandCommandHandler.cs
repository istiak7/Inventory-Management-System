using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Brands.Command.DeleteBrand
{
    // Deletes a brand that no product uses.
    public class DeleteBrandCommandHandler(
        AppDbContext _db,
        ILogger<DeleteBrandCommandHandler> _logger
    ) : IRequestHandler<DeleteBrandCommand, Result>
    {
        public async Task<Result> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.Brands.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Brand not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            if (await _db.Products.AnyAsync(x => x.BrandId == id, cancellationToken))
                return Fail(400, "Products use this brand. Move them to another brand first.");

            try
            {
                _db.Brands.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Brand deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Brand {Id}", id);
                return Fail(400, "Brand is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Brand {Id}", id);
                return Fail(500, "An error occurred while deleting the brand.");
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
