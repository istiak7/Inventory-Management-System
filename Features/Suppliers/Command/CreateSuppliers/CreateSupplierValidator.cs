using FluentValidation;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSuppliers
{
    public class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
    {
        public CreateSupplierValidator()
        {
            RuleFor(x => x.Group).NotEmpty().WithMessage("Group is required.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
            RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0).WithMessage("Opening balance must be greater than or equal to 0.");
        }
    }
}
