using FluentValidation;
using Inventory_Management_System.Features.Purchases.Shared;

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

            RuleFor(x => x.PaymentType)
                .Must(PurchasePaymentTypes.IsValid)
                .When(x => !string.IsNullOrWhiteSpace(x.PaymentType))
                .WithMessage($"Payment type must be one of: {PurchasePaymentTypes.Allowed}.");

            RuleFor(x => x.PaymentAmount)
                .GreaterThan(0)
                .When(x => x.PaymentAmount.HasValue)
                .WithMessage("Payment amount must be greater than 0.");
        }
    }
}
