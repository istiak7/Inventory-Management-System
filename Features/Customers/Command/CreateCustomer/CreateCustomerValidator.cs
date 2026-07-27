using FluentValidation;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomer
{
    public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
    {
        public CreateCustomerValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");

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
