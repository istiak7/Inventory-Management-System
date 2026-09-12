using FluentValidation;

namespace Inventory_Management_System.Features.Purchases.Command.UpdatePurchaseOrder
{
    public class UpdatePurchaseOrderValidator : AbstractValidator<UpdatePurchaseOrderCommand>
    {
        public UpdatePurchaseOrderValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("A valid purchase order id is required.");
            RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("SupplierId is required.");
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("BranchId is required.");

            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one purchase item is required.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductVariantId).GreaterThan(0).WithMessage("ProductVariantId is required.");
                item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
                item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be greater than or equal to 0.");
                item.RuleFor(i => i.WarrantyMonths).GreaterThanOrEqualTo(0).WithMessage("WarrantyMonths must be 0 or greater.");
            });
        }
    }
}
