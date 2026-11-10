using FluentValidation;
using Inventory_Management_System.Features.Quotations.Shared;

namespace Inventory_Management_System.Features.Quotations.Command.CreateQuotation
{
    public class CreateQuotationValidator : AbstractValidator<CreateQuotationCommand>
    {
        public CreateQuotationValidator()
        {
            Include(new SaveQuotationRules());
        }
    }
}
