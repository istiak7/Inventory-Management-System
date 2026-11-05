using FluentValidation;

namespace Inventory_Management_System.Features.Sales.Command.VoidSale
{
    public class VoidSaleValidator : AbstractValidator<VoidSaleCommand>
    {
        public VoidSaleValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Sale id is required.");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("A reason is required to void a sale.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
        }
    }
}
