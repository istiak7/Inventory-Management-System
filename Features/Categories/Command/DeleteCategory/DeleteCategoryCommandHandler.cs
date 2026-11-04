using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Categories.Command.DeleteCategory
{
    // Deletes a category that has no sub-categories.
    public class DeleteCategoryCommandHandler(
        AppDbContext _db,
        ILogger<DeleteCategoryCommandHandler> _logger
    ) : IRequestHandler<DeleteCategoryCommand, Result>
    {
        public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.ProductCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Category not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            if (await _db.ProductSubCategories.AnyAsync(x => x.ProductCategoryId == id, cancellationToken))
                return Fail(400, "This category still has sub-categories. Delete or move them first.");

            try
            {
                _db.ProductCategories.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Category deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Category {Id}", id);
                return Fail(400, "Category is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Category {Id}", id);
                return Fail(500, "An error occurred while deleting the category.");
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
