using FluentValidation;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Purchases.Command.EditPurchaseOrder
{
    public class EditPurchaseOrderValidator : AbstractValidator<EditPurchaseOrderCommand>
    {
        public EditPurchaseOrderValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("A valid purchase id is required.");
            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one purchase item is required.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Say why the purchase is being edited.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
            RuleFor(x => x.Remarks).MaximumLength(500)
                .When(x => x.Remarks != null)
                .WithMessage("Remarks must be 500 characters or fewer.");
            RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("DiscountAmount must be 0 or greater.").Money();
            RuleFor(x => x.AdditionalPayment).GreaterThanOrEqualTo(0).Money()
                .When(x => x.AdditionalPayment.HasValue)
                .WithMessage("Additional payment must be 0 or greater.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductVariantId).GreaterThan(0).WithMessage("ProductVariantId is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
                item.RuleFor(i => i.UnitPrice).GreaterThan(0).WithMessage("UnitPrice must be greater than 0.");
                item.RuleFor(i => i.UnitPrice).Money();
                item.RuleFor(i => i.WarrantyDays).GreaterThanOrEqualTo(0).WithMessage("WarrantyDays must be 0 or greater.");
            });
        }
    }
}
