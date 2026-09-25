using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.CreateExpenseCategories
{
    public class CreateExpenseCategoriesCommandHandler : IRequestHandler<CreateExpenseCategoriesCommand, Result>
    {
        private readonly ILogger<CreateExpenseCategoriesCommandHandler> _logger;
        private readonly IBaseRepository<ExpenseCategory> _expenseCategoriesRepository;
        private readonly AppDbContext _context;
        private readonly ICurrentUser _currentUser;

        public CreateExpenseCategoriesCommandHandler(AppDbContext context, ILogger<CreateExpenseCategoriesCommandHandler> logger,
            IBaseRepository<ExpenseCategory> expenseCategoriesRepository,
            ICurrentUser currentUser)
        {
            _logger = logger;
            _expenseCategoriesRepository = expenseCategoriesRepository;
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Result> Handle(CreateExpenseCategoriesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var expenseCategory = ExpenseCategory.Create(request.Name, request.Description);
                await _expenseCategoriesRepository.AddAsync(expenseCategory, cancellationToken);
                await _expenseCategoriesRepository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Expense category created successfully.");

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Created",
                    Message = "Expense category created successfully.",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating expense category.");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Internal Server Error",
                    Message = "An error occurred while creating the expense category.",
                    Data = null
                };
            }

        }
    }
}
