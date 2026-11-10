using FluentValidation;
using Inventory_Management_System.Features.Quotations.Shared.Dtos;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Quotations.Shared
{
    // Shared by create and update: both commands carry the same quotation body.
    public class SaveQuotationRules : AbstractValidator<SaveQuotationRequest>
    {
        public SaveQuotationRules()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("Customer is required.");
            RuleFor(x => x.Items).NotEmpty().WithMessage("Add at least one product.");

            RuleFor(x => x.ValidUntil)
                .Must(d => QuotationRules.ToStoredDate(d) >= QuotationRules.TodayStored)
                .WithMessage("Valid until cannot be in the past.");
            RuleFor(x => x)
                .Must(x => QuotationRules.ToStoredDate(x.ValidUntil) >=
                           QuotationRules.ToStoredDate(x.QuotationDate ?? QuotationRules.TodayStored))
                .WithName("ValidUntil")
                .WithMessage("Valid until must be on or after the quotation date.");

            RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("Discount must be 0 or greater.").Money();
            RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("Tax must be 0 or greater.").Money();
            RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("Notes must be 1000 characters or fewer.");
            RuleFor(x => x.TermsAndConditions).MaximumLength(4000)
                .WithMessage("Terms & conditions must be 4000 characters or fewer.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductVariantId).GreaterThan(0).WithMessage("Product is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
                item.RuleFor(i => i.UnitPrice).GreaterThan(0)
                    .When(i => i.UnitPrice.HasValue)
                    .WithMessage("Unit price must be greater than 0.");
                item.RuleFor(i => i.UnitPrice).Money();
                item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0)
                    .WithMessage("Line discount must be 0 or greater.").Money();
                item.RuleFor(i => i.Uom).MaximumLength(20).WithMessage("UoM must be 20 characters or fewer.");
            });
        }
    }
}
