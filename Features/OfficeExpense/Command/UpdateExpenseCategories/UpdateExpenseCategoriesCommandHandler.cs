using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.OfficeExpense.Command.UpdateExpense;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.UpdateExpenseCategories
{
    public class UpdateExpenseCategoriesCommandHandler : IRequestHandler<UpdateExpenseCategoriesCommand, Result>
    {
        private ILogger<UpdateExpenseCommandHandler> _logger;
        private readonly IBaseRepository<ExpenseCategory> _expenseRepository;

        public UpdateExpenseCategoriesCommandHandler(ILogger<UpdateExpenseCommandHandler> logger, IBaseRepository<ExpenseCategory> expenseRepository)
        {
            _logger = logger;
            _expenseRepository = expenseRepository;
        }

        public async Task<Result> Handle(UpdateExpenseCategoriesCommand request, CancellationToken cancellationToken)
        {
            var existingExpenseCategory = await _expenseRepository.GetByIdAsync(request.Id, cancellationToken);
            if (existingExpenseCategory == null)
            {
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 404,
                    Status = "Not Found",
                    Message = "Expense category not found",
                    Data = null
                };
            }

            try
            {
           
                existingExpenseCategory.UpdateCategory(request.Name, request.Description);
                await _expenseRepository.UpdateAsync(existingExpenseCategory, cancellationToken);
                await _expenseRepository.SaveChangesAsync(cancellationToken);
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Expense category updated successfully",
                    Data = existingExpenseCategory
                };
            }
         
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating expense category");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while updating the expense category",
                    Data = null
                };
            }

        }
    }
}
