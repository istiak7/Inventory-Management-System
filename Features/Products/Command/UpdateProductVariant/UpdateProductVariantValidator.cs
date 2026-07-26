using FluentValidation;

namespace Inventory_Management_System.Features.Products.Command.UpdateProductVariant
{
    public class UpdateProductVariantValidator : AbstractValidator<UpdateProductVariantCommand>
    {
        public UpdateProductVariantValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("A valid variant id is required.");
            RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.");
            RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).WithMessage("Selling price must be 0 or greater.");
        }
    }
}
