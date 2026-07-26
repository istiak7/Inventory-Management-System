using FluentValidation;

namespace Inventory_Management_System.Features.Products.Command.UpdateProduct
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.ProductName).NotEmpty().WithMessage("Product name is required.");
            RuleFor(x => x.ProductSubCategoryId).GreaterThan(0).WithMessage("A valid sub-category id is required.");
            RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("A valid brand id is required.");
        }
    }
}
