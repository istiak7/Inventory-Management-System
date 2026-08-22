using FluentValidation;

namespace Inventory_Management_System.Features.Roles.Command.CreateRole
{
    public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Role name is required")
                .MaximumLength(50).WithMessage("Role name must be at most 50 characters");
        }
    }
}
