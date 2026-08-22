using FluentValidation;

namespace Inventory_Management_System.Features.Users.Command.UpdateUser
{
    public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("User id is required");
            RuleFor(x => x.Username).NotEmpty().WithMessage("User name is required");
            RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("A role is required");
        }
    }
}
