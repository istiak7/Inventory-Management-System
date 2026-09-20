using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Repository;
using MediatR;

namespace Inventory_Management_System.Features.OfficeExpense.Command.CreateExpense
{
    public class CreateExpenseCommandHandler : IRequestHandler<CreateExpenseCommand, Result>
    {
        private readonly IBaseRepository<Expense> _expenseRepository;
        private readonly ILogger<CreateExpenseCommandHandler> _logger;
        private readonly AppDbContext _context;
        private readonly ICurrentUser _currentUser;
        public CreateExpenseCommandHandler(IBaseRepository<Expense> expenseRepository, ILogger<CreateExpenseCommandHandler> logger,
            AppDbContext context, ICurrentUser currentUser)
        {
            _expenseRepository = expenseRepository;
            _logger = logger;
            _context = context;
            _currentUser = currentUser;

        }
        public async Task<Result> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUserId = _currentUser.UserId??6;
                var expense = Expense.CreateExpense(request.Name, request.Description, request.Amount, request.BranchId, request.ExpenseCategoryId, request.ExpenseDate, "Cash", (int)currentUserId);
                await _expenseRepository.AddAsync(expense, cancellationToken);
                await _expenseRepository.SaveChangesAsync(cancellationToken);
                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Expense created successfully.",
                    Data = expense
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating expense");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while creating the expense.",
                    Data = null
                };
            }
        }
    }
}
