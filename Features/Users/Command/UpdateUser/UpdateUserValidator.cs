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
            // 0 = Active, 1 = Inactive. Deleting a user is not done through this endpoint.
            RuleFor(x => x.IsActive).InclusiveBetween(0, 1).WithMessage("Status must be Active (0) or Inactive (1)");
            RuleFor(x => x.NewPassword)
                .MinimumLength(6).WithMessage("New password must be at least 6 characters long")
                .When(x => !string.IsNullOrEmpty(x.NewPassword));
        }
    }
}
