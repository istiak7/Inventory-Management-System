using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.OfficeExpense.Command.DeleteExpenseCategory
{
    // Deletes an expense category no expense uses.
    public class DeleteExpenseCategoryCommandHandler(
        AppDbContext _db,
        ILogger<DeleteExpenseCategoryCommandHandler> _logger
    ) : IRequestHandler<DeleteExpenseCategoryCommand, Result>
    {
        public async Task<Result> Handle(DeleteExpenseCategoryCommand request, CancellationToken cancellationToken)
        {
            var id = request.Id;
            var entity = await _db.ExpenseCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null) return Fail(404, "Expense category not found.");

            // Checked across every branch (query filters off): a row in another branch still
            // depends on this record.
            if (await _db.Expenses.IgnoreQueryFilters().AnyAsync(x => x.ExpenseCategoryId == id, cancellationToken))
                return Fail(400, "Expenses use this category, so it is kept.");

            try
            {
                _db.ExpenseCategories.Remove(entity);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Expense category deleted successfully"
                };
            }
            catch (DbUpdateException ex)
            {
                // Something else still points at it (a reference the checks above do not know).
                _logger.LogWarning(ex, "Could not delete Expense category {Id}", id);
                return Fail(400, "Expense category is in use and cannot be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Expense category {Id}", id);
                return Fail(500, "An error occurred while deleting the expense category.");
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
