using FluentValidation;

namespace Inventory_Management_System.Features.Brands.Command.UpdateBrand
{
    public class UpdateBrandValidator : AbstractValidator<UpdateBrandCommand>
    {
        public UpdateBrandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Brand name is required.");
        }
    }
}
