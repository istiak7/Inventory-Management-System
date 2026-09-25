using FluentValidation;

namespace Inventory_Management_System.Features.OfficeExpense.Command.UpdateExpense
{
    public class UpdateExpenseValidator : AbstractValidator<UpdateExpenseCommand>
    {
        public UpdateExpenseValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Description).MaximumLength(255).WithMessage("Description cannot exceed 255 characters.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0.");
            RuleFor(x => x.ExpenseCategoryId).GreaterThan(0).WithMessage("Expense category is required.");
            RuleFor(x => x.ExpenseDate).NotEmpty().WithMessage("Expense date is required.");
        }
    }
}