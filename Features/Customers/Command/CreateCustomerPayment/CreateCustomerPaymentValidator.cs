using FluentValidation;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Customers.Command.CreateCustomerPayment
{
    public class CreateCustomerPaymentValidator : AbstractValidator<CreateCustomerPaymentCommand>
    {
        public CreateCustomerPaymentValidator()
        {
            RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("CustomerId is required.");
            RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("BranchId is required.");
            // Zero is allowed here so a payment can carry only discounts; the handler still rejects
            // a payment that has neither money nor a discount.
            RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithMessage("Payment amount cannot be negative.").Money();
            RuleFor(x => x.PaymentDate).NotInFuture();
            RuleForEach(x => x.Allocations).ChildRules(a =>
            {
                a.RuleFor(x => x.Amount).Money();
                a.RuleFor(x => x.Discount).Money();
            });
            RuleFor(x => x.PaymentMethod).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Payment method is required.").KnownPaymentMethod();
        }
    }
}
