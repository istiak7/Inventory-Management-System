using FluentValidation;

namespace Inventory_Management_System.Features.Purchases.Command.RejectPurchaseOrder
{
    public class RejectPurchaseOrderValidator : AbstractValidator<RejectPurchaseOrderCommand>
    {
        public RejectPurchaseOrderValidator()
        {
            RuleFor(x => x.PurchaseOrderId).GreaterThan(0).WithMessage("PurchaseOrderId is required.");
        }
    }
}
