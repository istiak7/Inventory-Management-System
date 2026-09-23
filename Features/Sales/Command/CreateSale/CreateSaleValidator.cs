using FluentValidation;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Features.Sales.Shared;
using Inventory_Management_System.Features.Customers.Shared;

namespace Inventory_Management_System.Features.Sales.Command.CreateSale
{
    public class CreateSaleValidator : AbstractValidator<CreateSaleCommand>
    {
        public CreateSaleValidator()
        {
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

            RuleFor(x => x.Remarks).MaximumLength(500)
                .When(x => x.Remarks != null)
                .WithMessage("Remarks must be 500 characters or fewer.");

            RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("DiscountAmount must be 0 or greater.").Money();
            RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("TaxAmount must be 0 or greater.").Money();
            RuleFor(x => x.SaleDate).NotInFuture();
            RuleFor(x => x.Payment!.Amount).Money().When(x => x.Payment != null);
            RuleFor(x => x.Payment!.PaymentDate).NotInFuture().When(x => x.Payment != null);

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductVariantId).GreaterThan(0).WithMessage("ProductVariantId is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
                // A typed price must be above 0: selling for nothing is done with a discount, which shows.
                item.RuleFor(i => i.UnitPrice).GreaterThan(0)
                    .When(i => i.UnitPrice.HasValue)
                    .WithMessage("UnitPrice must be greater than 0.");
                item.RuleFor(i => i.UnitPrice).Money();
                item.RuleFor(i => i.DiscountPerItem).GreaterThanOrEqualTo(0)
                    .When(i => i.DiscountPerItem.HasValue)
                    .WithMessage("DiscountPerItem must be 0 or greater.");
                item.RuleFor(i => i.DiscountPerItem).Money();
                item.RuleFor(i => i.WarrantyMonths).GreaterThanOrEqualTo(0)
                    .When(i => i.WarrantyMonths.HasValue)
                    .WithMessage("WarrantyMonths must be 0 or greater.");
                item.RuleFor(i => i.SerialNumber).MaximumLength(100)
                    .When(i => i.SerialNumber != null)
                    .WithMessage("SerialNumber must be 100 characters or fewer.");
            });

            RuleFor(x => x.PaymentType)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Payment type is required.")
                .Must(SalePaymentTypes.IsValid)
                .WithMessage($"Payment type must be one of: {SalePaymentTypes.Allowed}.");

            RuleFor(x => x.Payment!.Amount)
                .GreaterThan(0)
                .When(x => x.Payment?.Amount.HasValue == true)
                .WithMessage("Payment amount must be greater than 0.");
        }
    }
}
