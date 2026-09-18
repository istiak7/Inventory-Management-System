using FluentValidation;

namespace Inventory_Management_System.Features.OfficeExpense.Command.CreateExpense
{
    public class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
    {
        public CreateExpenseValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Expense name is required.")
                .MaximumLength(100).WithMessage("Expense name cannot exceed 100 characters.");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");
            RuleFor(x => x.BranchId)
                .GreaterThan(0).WithMessage("Branch ID must be greater than zero.");
            RuleFor(x => x.ExpenseCategoryId)
                .GreaterThan(0).WithMessage("Expense Category ID must be greater than zero.");
            RuleFor(x => x.ExpenseDate)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Expense date cannot be in the future.");
        }
    }
}
