using FluentValidation;

namespace Inventory_Management_System.Features.Warranty.Command.RejectWarrantyClaim
{
    public class RejectWarrantyClaimValidator : AbstractValidator<RejectWarrantyClaimCommand>
    {
        public RejectWarrantyClaimValidator()
        {
            RuleFor(x => x.WarrantyClaimId).GreaterThan(0);

            RuleFor(x => x.Reason).NotEmpty().MinimumLength(5).MaximumLength(1000)
                .WithMessage("Give a reason of at least 5 characters — the customer is told this when they collect the unit.");
        }
    }
}
