using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Categories.Command.DeleteSubCategory
{
    // Deletes a sub-category that no product uses.
    public class DeleteSubCategoryCommandHandler(
        AppDbContext _db,
        ILogger<DeleteSubCategoryCommandHandler> _logger
    ) : IRequestHandler<DeleteSubCategoryCommand, Result>
    {
        public async Task<Result> Handle(DeleteSubCategoryCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.ProductSubCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Sub-category not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            if (await _db.Products.AnyAsync(x => x.ProductSubCategoryId == id, cancellationToken))
                return Fail(400, "Products use this sub-category. Move them to another one first.");

            try
            {
                _db.ProductSubCategories.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Sub-category deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Sub-category {Id}", id);
                return Fail(400, "Sub-category is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Sub-category {Id}", id);
                return Fail(500, "An error occurred while deleting the sub-category.");
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
