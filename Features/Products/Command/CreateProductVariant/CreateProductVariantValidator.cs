using FluentValidation;

namespace Inventory_Management_System.Features.Products.Command.CreateProductVariant
{
    public class CreateProductVariantValidator : AbstractValidator<CreateProductVariantCommand>
    {
        public CreateProductVariantValidator()
        {
            RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("A valid product id is required.");
            RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.");
            RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling price must be 0 or greater.");
        }
    }
}
