using FluentValidation;

namespace Inventory_Management_System.Features.Activities.CreateActivity
{
   public class CreateActivityValidator : AbstractValidator<CreateActivityCommand>
    {
        public CreateActivityValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
        }
    }
}
