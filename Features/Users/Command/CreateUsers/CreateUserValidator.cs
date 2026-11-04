using FluentValidation;

namespace Inventory_Management_System.Features.Users.Command.CreateUsers
{
    public class CreateUserValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserValidator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("User name is required")
                .MaximumLength(50).WithMessage("User name must be at most 50 characters");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email format")
                .MaximumLength(100).WithMessage("Email must be at most 100 characters");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long");
            RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("A role is required");
        }
    }
}
