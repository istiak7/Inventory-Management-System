using FluentValidation;
using Inventory_Management_System.Features.Users.Login;

namespace Inventory_Management_System.Features.Users.Command.Login
{
    public class LoginUserValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserValidator()
        {
            RuleFor(x => x.Identifier).NotEmpty().WithMessage("User name or Email is required");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
            RuleFor(x => x.Password).MinimumLength(6).WithMessage("Password must be at least 6 characters long");
            RuleFor(x => x.Identifier).Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("User name or Email cannot be empty or whitespace");
        }
    }
}
