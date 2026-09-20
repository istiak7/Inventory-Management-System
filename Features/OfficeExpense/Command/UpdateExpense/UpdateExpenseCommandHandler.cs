using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.UpdateExpense
{
    public class UpdateExpenseCommandHandler : IRequestHandler<UpdateExpenseCommand, Result>
    {
        private ILogger<UpdateExpenseCommandHandler> _logger;
        private readonly IBaseRepository<Expense> _expenseRepository;
        public UpdateExpenseCommandHandler(ILogger<UpdateExpenseCommandHandler> logger, IBaseRepository<Expense> expenseRepository)
        {
            _logger = logger;
            _expenseRepository = expenseRepository;
        }

        public async Task<Result> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
        {
            var existingExpense = await _expenseRepository.GetByIdAsync(request.Id, cancellationToken);
            if(existingExpense == null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = "Expense not found.",
                    Data = null
                };
            }
            try
            {
                existingExpense.UpdateExpense(request.Name, request.Description, request.Amount, request.ExpenseCategoryId, request.ExpenseDate, "Cash");
                await _expenseRepository.UpdateAsync(existingExpense, cancellationToken);
                await _expenseRepository.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Expense updated successfully.",
                    Data = existingExpense
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating expense.");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the expense.",
                    Data = null
                };
            }
        }
    }
}
