using FluentValidation;
using Inventory_Management_System.Features.Customers.Shared;

namespace Inventory_Management_System.Features.Suppliers.Command.UpdateSupplier
{
    public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierCommand>
    {
        public UpdateSupplierValidator()
        {
            RuleFor(x => x.Group).NotEmpty().WithMessage("Group is required.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(CustomerPhoneNumber.IsValid)
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage($"Phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits.");
            // Optional: a supplier made from a customer often has no email or NID.
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("A valid email is required.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0).WithMessage("Opening balance must be 0 or greater.");
        }
    }
}
