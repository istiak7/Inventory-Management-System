using FluentValidation;

namespace Inventory_Management_System.Features.Purchases.Command.ApprovePurchaseOrder
{
    public class ApprovePurchaseOrderValidator : AbstractValidator<ApprovePurchaseOrderCommand>
    {
        public ApprovePurchaseOrderValidator()
        {
            RuleFor(x => x.PurchaseOrderId).GreaterThan(0).WithMessage("PurchaseOrderId is required.");

            When(x => x.Payment != null, () =>
            {
                RuleFor(x => x.Payment!.Amount).GreaterThanOrEqualTo(0).WithMessage("Payment amount must be greater than or equal to 0.");
                RuleFor(x => x.Payment!.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
            });
        }
    }
}
