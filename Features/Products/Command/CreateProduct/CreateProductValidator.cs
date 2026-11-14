using FluentValidation;

namespace Inventory_Management_System.Features.Products.Command.CreateProduct
{
    public class CreateProductValidator : AbstractValidator<CreateProductCommand>
    {
        public CreateProductValidator()
        {
            RuleFor(x => x.ProductName).NotEmpty().WithMessage("Product name is required.");
            RuleFor(x => x.Warranty).MaximumLength(100).WithMessage("Warranty must be 100 characters or fewer.");
            RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling price must be 0 or greater.");
            RuleFor(x => x.ProductSubCategoryId).GreaterThan(0).WithMessage("A valid sub-category id is required.");
            RuleFor(x => x.BrandId).GreaterThan(0).WithMessage("A valid brand id is required.");
        }
    }
}
