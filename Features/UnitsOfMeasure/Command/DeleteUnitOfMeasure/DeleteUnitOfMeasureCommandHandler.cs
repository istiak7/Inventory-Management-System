using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.DeleteUnitOfMeasure
{
    // Deletes a unit that no product uses. A unit in use is refused: make it inactive instead.
    public class DeleteUnitOfMeasureCommandHandler(
        AppDbContext _db,
        ILogger<DeleteUnitOfMeasureCommandHandler> _logger
    ) : IRequestHandler<DeleteUnitOfMeasureCommand, Result>
    {
        public async Task<Result> Handle(DeleteUnitOfMeasureCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var unit = await _db.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (unit is null) return Fail(404, "Unit of measurement not found.");

            var used = await _db.Products.CountAsync(p => p.UnitOfMeasureId == id, cancellationToken);
            if (used > 0)
                return Fail(400, $"{used} product(s) use this unit. Make it inactive instead of deleting it.");

            try
            {
                _db.UnitOfMeasures.Remove(unit);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Unit of measurement deleted successfully",
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Could not delete unit of measurement {Id}", id);
                return Fail(400, "This unit is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting unit of measurement {Id}", id);
                return Fail(500, "An error occurred while deleting the unit of measurement.");
            }
        }

        private static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message,
        };
    }
}
