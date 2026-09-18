using FluentValidation;

namespace Inventory_Management_System.Features.OfficeExpense.Command.CreateExpenseCategories
{
    public class CreateExpenseCategoriesValidator : AbstractValidator<CreateExpenseCategoriesCommand>
    {
        public CreateExpenseCategoriesValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }
    }
}
