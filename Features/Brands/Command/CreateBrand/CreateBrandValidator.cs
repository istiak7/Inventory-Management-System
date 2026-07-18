using FluentValidation;

namespace Inventory_Management_System.Features.Brands.Command.CreateBrand
{
    public class CreateBrandValidator : AbstractValidator<CreateBrandCommand>
    {
        public CreateBrandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Brand name is required.");
        }
    }
}
