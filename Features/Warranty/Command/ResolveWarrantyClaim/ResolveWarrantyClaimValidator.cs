using FluentValidation;

namespace Inventory_Management_System.Features.Warranty.Command.ResolveWarrantyClaim
{
    public class ResolveWarrantyClaimValidator : AbstractValidator<ResolveWarrantyClaimCommand>
    {
        private static readonly string[] Allowed = ["Repaired", "Replaced"];

        public ResolveWarrantyClaimValidator()
        {
            RuleFor(x => x.WarrantyClaimId).GreaterThan(0);

            RuleFor(x => x.Resolution)
                .NotEmpty()
                .Must(r => Allowed.Contains(r, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Resolution must be either 'Repaired' or 'Replaced'. Use the reject endpoint for a unit that cannot be fixed.");

            RuleFor(x => x.ReplacementSerialNumber)
                .NotEmpty().MaximumLength(100)
                .When(x => string.Equals(x.Resolution, "Replaced", StringComparison.OrdinalIgnoreCase))
                .WithMessage("A replacement serial number is required when resolving as Replaced.");

            RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
        }
    }
}
