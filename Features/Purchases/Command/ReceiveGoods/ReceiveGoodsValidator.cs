using FluentValidation;

namespace Inventory_Management_System.Features.Purchases.Command.ReceiveGoods
{
    public class ReceiveGoodsValidator : AbstractValidator<ReceiveGoodsCommand>
    {
        public ReceiveGoodsValidator()
        {
            RuleFor(x => x.PurchaseOrderId).GreaterThan(0).WithMessage("A valid purchase order id is required.");
            RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line must be received.");
            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.SupplierPurchaseDetailsId).GreaterThan(0).WithMessage("A valid line id is required.");
            });

            When(x => x.Payment != null, () =>
            {
                RuleFor(x => x.Payment!.Amount).GreaterThanOrEqualTo(0).WithMessage("Payment amount must be greater than or equal to 0.");
                RuleFor(x => x.Payment!.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
            });
        }
    }
}
