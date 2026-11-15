using FluentValidation;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.CreateUnitOfMeasure
{
    public class CreateUnitOfMeasureValidator : AbstractValidator<CreateUnitOfMeasureCommand>
    {
        public CreateUnitOfMeasureValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Unit name is required.")
                .MaximumLength(100).WithMessage("Unit name must be 100 characters or fewer.");
            RuleFor(x => x.ShortName).NotEmpty().WithMessage("Short name is required.")
                .MaximumLength(20).WithMessage("Short name must be 20 characters or fewer.");
            RuleFor(x => x.Description).MaximumLength(500).WithMessage("Description must be 500 characters or fewer.");
        }
    }
}
