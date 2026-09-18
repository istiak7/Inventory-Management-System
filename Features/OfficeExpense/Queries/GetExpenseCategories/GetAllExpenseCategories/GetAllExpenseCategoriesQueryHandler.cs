using Inventory_Management_System.Database;
using Inventory_Management_System.Features.OfficeExpense.Queries.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.OfficeExpense.Queries.GetExpenseCategories.GetAllExpenseCategories
{
    public class GetAllExpenseCategoriesQueryHandler : IRequestHandler<GetAllExpenseCategoriesQuery, Result>
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<GetAllExpenseCategoriesQueryHandler> _logger;
        private readonly ICurrentUser _currentUser;

        public GetAllExpenseCategoriesQueryHandler(AppDbContext dbContext, ILogger<GetAllExpenseCategoriesQueryHandler> logger, ICurrentUser currentUser)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(GetAllExpenseCategoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var expenseCategories = await _dbContext.ExpenseCategories
                    .Where(ec => ec.IsActive == 1)
                    .Select(ec => new ExpenseCategoriesResponse
                    {
                        Name = ec.Name,
                        Description = ec.Description
                    }).ToListAsync(cancellationToken);

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
