using FluentValidation;

namespace Inventory_Management_System.Features.Transfers.Command.CreateStockTransfer
{
    public class CreateStockTransferValidator : AbstractValidator<CreateStockTransferCommand>
    {
        public CreateStockTransferValidator()
        {
            RuleFor(x => x.SourceBranchId).GreaterThan(0).WithMessage("SourceBranchId is required.");
            RuleFor(x => x.DestinationBranchId).GreaterThan(0).WithMessage("DestinationBranchId is required.");

            RuleFor(x => x)
                .Must(x => x.SourceBranchId != x.DestinationBranchId)
                .WithMessage("Source and destination branch must be different.");

            RuleFor(x => x.Items).NotEmpty().WithMessage("At least one transfer item is required.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductVariantId).GreaterThan(0).WithMessage("ProductVariantId is required.");

                item.RuleFor(i => i.Quantity).GreaterThan(0)
                    .When(i => i.Quantity.HasValue)
                    .WithMessage("Quantity must be greater than 0.");

                item.RuleForEach(i => i.SerialNumbers).NotEmpty().MaximumLength(100)
                    .When(i => i.SerialNumbers != null)
                    .WithMessage("Each serial number must be 1-100 characters.");
            });

            RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes != null);
        }
    }
}
