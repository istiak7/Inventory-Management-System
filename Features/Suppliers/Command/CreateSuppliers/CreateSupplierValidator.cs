using FluentValidation;
using Inventory_Management_System.Features.Customers.Shared;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSuppliers
{
    public class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
    {
        public CreateSupplierValidator()
        {
            RuleFor(x => x.Group).NotEmpty().WithMessage("Group is required.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(CustomerPhoneNumber.IsValid)
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .WithMessage($"Phone number must contain at least {CustomerPhoneNumber.MinimumDigits} digits.");
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("A valid email is required.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0).WithMessage("Opening balance must be greater than or equal to 0.");
        }
    }
}
