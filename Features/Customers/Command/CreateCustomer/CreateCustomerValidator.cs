using FluentValidation;
using Inventory_Management_System.Features.Customers.Shared;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomer
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");

            // Checked against the normalized form, because that is what gets stored and what the
            // unique index sees — "(017) 12" looks long enough until the punctuation comes off.
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(CustomerPhoneNumber.IsValid)
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage($"Phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits.");

            // Unlike suppliers, email is optional — a walk-in customer often has none. Validate the
            // format only when something was actually supplied.
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("A valid email is required.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.OpeningBalance)
                .GreaterThanOrEqualTo(0).WithMessage("Opening balance must be greater than or equal to 0.");
        }
    }
}
