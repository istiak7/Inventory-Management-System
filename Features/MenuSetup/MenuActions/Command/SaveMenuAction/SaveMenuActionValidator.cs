using FluentValidation;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Command.SaveMenuAction
{
    public class SaveMenuActionValidator : AbstractValidator<SaveMenuActionCommand>
    {
        public SaveMenuActionValidator()
        {
            RuleFor(x => x.MenuId).GreaterThan(0).WithMessage("Please choose a menu");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name should be less than 200 characters");
            RuleFor(x => x.ComponentCode).NotEmpty().WithMessage("Component Code is required")
                .MaximumLength(50).WithMessage("Component Code should be less than 50 characters");
            RuleFor(x => x.ApiRoutes).NotEmpty().WithMessage("Add at least one API route");
        }
    }
}
