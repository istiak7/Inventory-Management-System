using FluentValidation;

namespace Inventory_Management_System.Features.Suppliers.Command.UpdateSupplier
{
    public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierCommand>
    {
        public UpdateSupplierValidator()
        {
            RuleFor(x => x.Group).NotEmpty().WithMessage("Group is required.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
            RuleFor(x => x.NID).NotEmpty().WithMessage("NID is required.");
            RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0).WithMessage("Opening balance must be 0 or greater.");
        }
    }
}
