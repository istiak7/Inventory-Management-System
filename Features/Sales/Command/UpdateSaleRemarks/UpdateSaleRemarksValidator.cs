using FluentValidation;

namespace Inventory_Management_System.Features.Sales.Command.UpdateSaleRemarks
{
    public class UpdateSaleRemarksValidator : AbstractValidator<UpdateSaleRemarksCommand>
    {
        public UpdateSaleRemarksValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Sale id is required.");

            RuleFor(x => x.Remarks).MaximumLength(500)
                .When(x => x.Remarks != null)
                .WithMessage("Remarks must be 500 characters or fewer.");
        }
    }
}
