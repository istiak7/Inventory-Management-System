using FluentValidation;

namespace Inventory_Management_System.Features.Categories.Command.UpdateCategory
{
    public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryValidator()
        {
            RuleFor(x => x.CategoryName).NotEmpty().WithMessage("Category name is required.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.");
        }
    }
}
