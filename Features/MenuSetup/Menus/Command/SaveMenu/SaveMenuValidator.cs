using FluentValidation;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Command.SaveMenu
{
    public class SaveMenuValidator : AbstractValidator<SaveMenuCommand>
    {
        public SaveMenuValidator()
        {
            RuleFor(x => x.SubmoduleId).GreaterThan(0).WithMessage("Please choose submodule");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Menu name is required")
                .MaximumLength(200).WithMessage("Menu name should be less than 200 characters");
            RuleFor(x => x.FrontendRoute).NotEmpty().WithMessage("Frontend Route is required")
                .When(x => x.IsLeaf);
            RuleFor(x => x.FrontendRoute).MaximumLength(200).WithMessage("Frontend Route should be less than 200 characters")
                .Must(r => string.IsNullOrWhiteSpace(r) || r.Trim().StartsWith('/')).WithMessage("Frontend Route must start with '/', e.g. /products");
            RuleFor(x => x.MenuOrder).GreaterThanOrEqualTo(0).WithMessage("Menu Order cannot be negative");
            RuleFor(x => x.Icon).MaximumLength(50).WithMessage("Icon should be less than 50 characters");
            RuleFor(x => x.Permission).MaximumLength(100).WithMessage("Permission should be less than 100 characters");
        }
    }
}
