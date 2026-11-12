using FluentValidation;

namespace Inventory_Management_System.Features.SalesReturns.Command.CreateSalesReturn
{
    public class CreateSalesReturnValidator : AbstractValidator<CreateSalesReturnCommand>
    {
        public CreateSalesReturnValidator()
        {
            RuleFor(x => x.SaleId).GreaterThan(0).WithMessage("Choose the sales invoice to return from.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason for the return.")
                .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
            RuleFor(x => x.Remarks).MaximumLength(500).WithMessage("Remarks must be 500 characters or fewer.");
            RuleFor(x => x.Lines).NotEmpty().WithMessage("Choose at least one product to return.");
            RuleForEach(x => x.Lines).ChildRules(line =>
            {
                line.RuleFor(l => l.SaleDetailsId).GreaterThan(0).WithMessage("Each returned line must point to a sale line.");
                line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Return quantity must be more than 0.");
            });
            RuleFor(x => x.Lines)
                .Must(lines => lines.Select(l => l.SaleDetailsId).Distinct().Count() == lines.Count)
                .WithMessage("The same product line is listed twice.");
        }
    }
}
