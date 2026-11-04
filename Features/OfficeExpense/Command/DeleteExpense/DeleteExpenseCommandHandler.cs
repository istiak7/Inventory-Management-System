using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.OfficeExpense.Command.DeleteExpense
{
    // Deletes an office expense entry (nothing else refers to an expense).
    public class DeleteExpenseCommandHandler(
        AppDbContext _db,
        ILogger<DeleteExpenseCommandHandler> _logger
    ) : IRequestHandler<DeleteExpenseCommand, Result>
    {
        public async Task<Result> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.Expenses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Expense not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.

            try
            {
                _db.Expenses.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Expense deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Expense {Id}", id);
                return Fail(400, "Expense is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Expense {Id}", id);
                return Fail(500, "An error occurred while deleting the expense.");
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
