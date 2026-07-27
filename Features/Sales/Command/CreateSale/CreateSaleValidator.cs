using FluentValidation;

namespace Inventory_Management_System.Features.Sales.Command.CreateSale
{
    public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
    {
        public CreateSaleValidator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("CustomerId is required.");
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("BranchId is required.");

            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one sale item is required.");

            RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("DiscountAmount must be 0 or greater.");
            RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("TaxAmount must be 0 or greater.");

            // No UnitPrice rule on purpose — price is resolved server-side, never accepted here.
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductVariantId).GreaterThan(0).WithMessage("ProductVariantId is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
                item.RuleFor(i => i.DiscountPerItem).GreaterThanOrEqualTo(0)
                    .When(i => i.DiscountPerItem.HasValue)
                    .WithMessage("DiscountPerItem must be 0 or greater.");
                item.RuleFor(i => i.WarrantyMonths).GreaterThanOrEqualTo(0)
                    .When(i => i.WarrantyMonths.HasValue)
                    .WithMessage("WarrantyMonths must be 0 or greater.");
            });

            When(x => x.Payment != null, () =>
            {
                RuleFor(x => x.Payment!.Amount).GreaterThanOrEqualTo(0).WithMessage("Payment amount must be greater than or equal to 0.");
                RuleFor(x => x.Payment!.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
            });
        }
    }
}
