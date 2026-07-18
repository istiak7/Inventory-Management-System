using FluentValidation;

namespace Inventory_Management_System.Features.Categories.Command.CreateCategory
{
    public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryValidator()
        {
            RuleFor(x => x.CategoryName).NotEmpty().WithMessage("Category name is required.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.");
        }
    }
}
