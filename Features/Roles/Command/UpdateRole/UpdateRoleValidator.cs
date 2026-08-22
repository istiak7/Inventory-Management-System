using FluentValidation;

namespace Inventory_Management_System.Features.Roles.Command.UpdateRole
{
    public class UpdateRoleValidator : AbstractValidator<UpdateRoleCommand>
    {
        public UpdateRoleValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Role id is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Role name is required")
                .MaximumLength(50).WithMessage("Role name must be at most 50 characters");
        }
    }
}
