using FluentValidation;
using Inventory_Management_System.Features.Quotations.Shared;

namespace Inventory_Management_System.Features.Quotations.Command.UpdateQuotation
{
    public class UpdateQuotationValidator : AbstractValidator<UpdateQuotationCommand>
    {
        public UpdateQuotationValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            Include(new SaveQuotationRules());
        }
    }
}
