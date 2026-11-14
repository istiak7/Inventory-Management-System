using FluentValidation;

namespace Inventory_Management_System.Features.Sales.Command.CancelSale
{
    public class CancelSaleValidator : AbstractValidator<CancelSaleCommand>
    {
        public CancelSaleValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("A valid sale id is required.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Say why the invoice is being deleted.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
        }
    }
}
