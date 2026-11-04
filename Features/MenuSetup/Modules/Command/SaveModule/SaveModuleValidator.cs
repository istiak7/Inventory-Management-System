using FluentValidation;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Command.SaveModule
{
    public class SaveModuleValidator : AbstractValidator<SaveModuleCommand>
    {
        public SaveModuleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Module name is required")
                .MaximumLength(200).WithMessage("Module name should be less than 200 characters");
            RuleFor(x => x.Icon).MaximumLength(50).WithMessage("Icon should be less than 50 characters");
            RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative");
        }
    }
}
