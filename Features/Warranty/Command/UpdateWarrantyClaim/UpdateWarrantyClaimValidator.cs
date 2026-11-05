using FluentValidation;

namespace Inventory_Management_System.Features.Warranty.Command.UpdateWarrantyClaim
{
    public class UpdateWarrantyClaimValidator : AbstractValidator<UpdateWarrantyClaimCommand>
    {
        public UpdateWarrantyClaimValidator()
        {
            RuleFor(x => x.DefectDescription).NotEmpty().MinimumLength(5).MaximumLength(1000)
                .WithMessage("Describe the defect in at least 5 characters.");

            RuleFor(x => x.AccessoriesReceived).MaximumLength(500).When(x => x.AccessoriesReceived != null);
            RuleFor(x => x.TechnicianName).MaximumLength(100).When(x => x.TechnicianName != null);
        }
    }
}
