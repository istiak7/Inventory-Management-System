using FluentValidation;

namespace Inventory_Management_System.Features.OfficeExpense.Command.UpdateExpenseCategories
{
    public class UpdateExpenseCategoriesValidator : AbstractValidator<UpdateExpenseCategoriesCommand>
    {
        public UpdateExpenseCategoriesValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Id is required.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }
    }
}
