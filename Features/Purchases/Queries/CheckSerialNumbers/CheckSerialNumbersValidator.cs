using FluentValidation;

namespace Inventory_Management_System.Features.Purchases.Queries.CheckSerialNumbers
{
    public class CheckSerialNumbersValidator : AbstractValidator<CheckSerialNumbersQuery>
    {
        public const int MaxSerials = 1000;

        public CheckSerialNumbersValidator()
        {
            RuleFor(x => x.SerialNumbers)
                .Must(s => s.Count <= MaxSerials)
                .WithMessage($"At most {MaxSerials} serial numbers can be checked at once.");
        }
    }
}
