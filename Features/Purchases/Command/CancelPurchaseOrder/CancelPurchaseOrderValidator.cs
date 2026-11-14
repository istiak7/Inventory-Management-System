using FluentValidation;

namespace Inventory_Management_System.Features.Purchases.Command.CancelPurchaseOrder
{
    public class CancelPurchaseOrderValidator : AbstractValidator<CancelPurchaseOrderCommand>
    {
        public CancelPurchaseOrderValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("A valid purchase id is required.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Say why the purchase is being deleted.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
        }
    }
}
