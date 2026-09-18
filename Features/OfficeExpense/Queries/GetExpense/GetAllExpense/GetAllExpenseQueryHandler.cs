using Inventory_Management_System.Database;
using Inventory_Management_System.Features.OfficeExpense.Queries.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.OfficeExpense.Queries.GetExpense.GetAllExpense
{
    public class GetAllExpenseQueryHandler : IRequestHandler<GetAllExpenseQuery, Result>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<GetAllExpenseQueryHandler> _logger;
        private readonly ICurrentUser _currentUser;
        public GetAllExpenseQueryHandler(AppDbContext dbContext, ILogger<GetAllExpenseQueryHandler> logger, ICurrentUser currentUser)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUser = currentUser;
        }
        public async Task<Result> Handle(GetAllExpenseQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var expenseCategories = await _dbContext.Expenses
                                 .Include(e => e.ExpenseCategory)
                                 .Where(e => e.ExpenseCategory.Id == e.ExpenseCategoryId)
                                 .Select(e => new ExpenseResponse()
                                 {
                                     Expensecategories = e.ExpenseCategory.Name,
                                     ExpenseName = e.Name,
                                     Description = e.Description,
                                     Amount = e.Amount,
                                     ExpenseDate = e.ExpenseDate,
                                     BranchName = e.Branch.Name,
                                     EntryBy = e.RecordByUserId.ToString()
                                 })
                                 .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "OK",
                    Message = "Expense categories fetched successfully.",
                    Data = expenseCategories
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching expense categories.");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Internal Server Error",
                    Message = "An error occurred while fetching the expense categories.",
                    Data = null
                };
            }
        }
    }
}
