using FluentValidation;
using Inventory_Management_System.Features.Customers.Shared;

namespace Inventory_Management_System.Features.Sales.Command.CreateSale
{
    public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
    {
        public CreateSaleValidator()
        {
            // A sale needs exactly one way of naming its customer. Both at once is ambiguous —
            // it would leave the server guessing whether to bill the id or the phone number.
            RuleFor(x => x)
                .Must(x => (x.CustomerId is > 0) ^ (x.Customer != null))
                .WithMessage("Provide exactly one of CustomerId or Customer details.");

            When(x => x.Customer != null, () =>
            {
                RuleFor(x => x.Customer!.Name)
                    .NotEmpty().WithMessage("Customer name is required.");

                RuleFor(x => x.Customer!.PhoneNumber)
                    .NotEmpty().WithMessage("Customer phone number is required.")
                    .Must(CustomerPhoneNumber.IsValid)
                    .When(x => !string.IsNullOrWhiteSpace(x.Customer!.PhoneNumber))
                    .WithMessage($"Customer phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits.");

                RuleFor(x => x.Customer!.Email)
                    .EmailAddress().WithMessage("A valid customer email is required.")
                    .When(x => !string.IsNullOrWhiteSpace(x.Customer!.Email));
            });

            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("BranchId is required.");

            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one sale item is required.");

            // Bounded to the column so an over-long note fails as a clean 400 instead of a DB error.
            RuleFor(x => x.Remarks).MaximumLength(500)
                .When(x => x.Remarks != null)
                .WithMessage("Remarks must be 500 characters or fewer.");

            RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("DiscountAmount must be 0 or greater.");
            RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("TaxAmount must be 0 or greater.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductVariantId).GreaterThan(0).WithMessage("ProductVariantId is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
                // UnitPrice is optional: null means "use the variant's catalog price". When the
                // seller does type a price, it only has to be a sane amount.
                item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0)
                    .When(i => i.UnitPrice.HasValue)
                    .WithMessage("UnitPrice must be 0 or greater.");
                item.RuleFor(i => i.DiscountPerItem).GreaterThanOrEqualTo(0)
                    .When(i => i.DiscountPerItem.HasValue)
                    .WithMessage("DiscountPerItem must be 0 or greater.");
                item.RuleFor(i => i.WarrantyMonths).GreaterThanOrEqualTo(0)
                    .When(i => i.WarrantyMonths.HasValue)
                    .WithMessage("WarrantyMonths must be 0 or greater.");
                // Whether a serial is actually required depends on ProductVariant.IsSerialized,
                // which only the handler knows — this just bounds the length to the column.
                item.RuleFor(i => i.SerialNumber).MaximumLength(100)
                    .When(i => i.SerialNumber != null)
                    .WithMessage("SerialNumber must be 100 characters or fewer.");
            });

            When(x => x.Payment != null, () =>
            {
                RuleFor(x => x.Payment!.Amount).GreaterThanOrEqualTo(0).WithMessage("Payment amount must be greater than or equal to 0.");
                RuleFor(x => x.Payment!.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
            });
        }
    }
}
