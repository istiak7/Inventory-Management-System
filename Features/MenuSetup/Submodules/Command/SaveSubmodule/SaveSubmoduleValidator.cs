using FluentValidation;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Command.SaveSubmodule
{
    public class SaveSubmoduleValidator : AbstractValidator<SaveSubmoduleCommand>
    {
        public SaveSubmoduleValidator()
        {
            RuleFor(x => x.ModuleId).GreaterThan(0).WithMessage("Please choose module");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Submodule name is required")
                .MaximumLength(200).WithMessage("Submodule name should be less than 200 characters");
            RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative");
        }
    }
}
