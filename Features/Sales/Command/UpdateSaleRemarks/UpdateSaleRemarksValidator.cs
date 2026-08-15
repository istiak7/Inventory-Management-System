using FluentValidation;

namespace Inventory_Management_System.Features.Sales.Command.UpdateSaleRemarks
{
    public class UpdateSaleRemarksValidator : AbstractValidator<UpdateSaleRemarksCommand>
    {
        public UpdateSaleRemarksValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Sale id is required.");

            // Bounded to the column so an over-long note fails as a clean 400 instead of a DB error.
            RuleFor(x => x.Remarks).MaximumLength(500)
                .When(x => x.Remarks != null)
                .WithMessage("Remarks must be 500 characters or fewer.");
        }
    }
}
