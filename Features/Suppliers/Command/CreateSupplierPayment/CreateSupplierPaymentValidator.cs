using FluentValidation;

namespace Inventory_Management_System.Features.Suppliers.Command.CreateSupplierPayment
{
    public class CreateSupplierPaymentValidator : AbstractValidator<CreateSupplierPaymentCommand>
    {
        public CreateSupplierPaymentValidator()
        {
            RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("SupplierId is required.");
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("BranchId is required.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than 0.");
            RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
        }
    }
}
