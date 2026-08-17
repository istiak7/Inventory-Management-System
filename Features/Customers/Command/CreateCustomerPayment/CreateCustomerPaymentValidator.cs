using FluentValidation;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomerPayment
{
    public class CreateCustomerPaymentValidator : AbstractValidator<CreateCustomerPaymentCommand>
    {
        public CreateCustomerPaymentValidator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("CustomerId is required.");
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("BranchId is required.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than 0.");
            RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
        }
    }
}
