using FluentValidation;

namespace Inventory_Management_System.Features.Warranty.Command.CreateWarrantyClaim
{
    public class CreateWarrantyClaimValidator : AbstractValidator<CreateWarrantyClaimCommand>
    {
        public CreateWarrantyClaimValidator()
        {
            RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(100)
                .WithMessage("A serial number is required.");

            RuleFor(x => x.DefectDescription).NotEmpty().MinimumLength(5).MaximumLength(1000)
                .WithMessage("Describe the defect in at least 5 characters.");

            RuleFor(x => x.AccessoriesReceived).MaximumLength(500).When(x => x.AccessoriesReceived != null);
            RuleFor(x => x.TechnicianName).MaximumLength(100).When(x => x.TechnicianName != null);
            RuleFor(x => x.BranchId).GreaterThan(0).When(x => x.BranchId.HasValue)
                .WithMessage("BranchId must be a valid branch.");
        }
    }
}
