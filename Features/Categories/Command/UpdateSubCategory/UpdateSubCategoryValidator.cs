using FluentValidation;

namespace Inventory_Management_System.Features.Categories.Command.UpdateSubCategory
{
    public class UpdateSubCategoryValidator : AbstractValidator<UpdateSubCategoryCommand>
    {
        public UpdateSubCategoryValidator()
        {
            RuleFor(x => x.SubCategoryName).NotEmpty().WithMessage("Sub-category name is required.");
            RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required.");
            RuleFor(x => x.ProductCategoryId).GreaterThan(0).WithMessage("A valid category id is required.");
        }
    }
}
