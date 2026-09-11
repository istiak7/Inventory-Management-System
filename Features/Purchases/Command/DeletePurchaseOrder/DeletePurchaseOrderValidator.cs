using FluentValidation;

namespace Inventory_Management_System.Features.Purchases.Command.DeletePurchaseOrder
{
    public class DeletePurchaseOrderValidator : AbstractValidator<DeletePurchaseOrderCommand>
    {
        public DeletePurchaseOrderValidator()
        {
            RuleFor(x => x.PurchaseOrderId).GreaterThan(0).WithMessage("PurchaseOrderId is required.");
        }
    }
}
