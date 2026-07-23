using FluentValidation;

namespace Inventory_Management_System.Features.Branches.Command.CreateBranch
{
    public class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
    {
        public CreateBranchValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
            RuleFor(x => x.Location).MaximumLength(200).WithMessage("Location must not exceed 200 characters.");
            RuleFor(x => x.PhoneNumber).MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.");
            RuleFor(x => x.Email).MaximumLength(100).WithMessage("Email must not exceed 100 characters.");
            When(x => !string.IsNullOrEmpty(x.Email), () =>
            {
                RuleFor(x => x.Email).EmailAddress().WithMessage("A valid email is required.");
            });
        }
    }
}
