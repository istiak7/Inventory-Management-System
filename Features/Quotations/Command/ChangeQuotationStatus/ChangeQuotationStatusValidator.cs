using FluentValidation;
using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Features.Quotations.Command.ChangeQuotationStatus
{
    public class ChangeQuotationStatusValidator : AbstractValidator<ChangeQuotationStatusCommand>
    {
        // Converted is not here: it is set only by converting the quotation into a sale.
        private static readonly string[] Allowed =
        [
            nameof(QuotationStatus.Draft), nameof(QuotationStatus.Sent), nameof(QuotationStatus.Accepted),
            nameof(QuotationStatus.Rejected), nameof(QuotationStatus.Expired),
        ];

        public ChangeQuotationStatusValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Status)
                .Must(s => Allowed.Contains(s, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Status must be one of: {string.Join(", ", Allowed)}.");
        }
    }
}
