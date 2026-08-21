using FluentValidation;

namespace Inventory_Management_System.Features.Products.Queries.SearchProducts
{
    public class SearchProductsValidator : AbstractValidator<SearchProductsQuery>
    {
        public const int MaxPageSize = 100;
        public const int MaxTermLength = 200;

        public SearchProductsValidator()
        {
            RuleFor(x => x.Term)
                .MaximumLength(MaxTermLength)
                .WithMessage($"Search term must be {MaxTermLength} characters or fewer.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be 1 or greater.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, MaxPageSize)
                .WithMessage($"Page size must be between 1 and {MaxPageSize}.");

            RuleFor(x => x.ProductId)
                .GreaterThan(0).When(x => x.ProductId.HasValue)
                .WithMessage("A valid product id is required.");

            RuleFor(x => x.BranchId)
                .GreaterThan(0).When(x => x.BranchId.HasValue)
                .WithMessage("A valid branch id is required.");

            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
                .WithMessage("Minimum price must be 0 or greater.");

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue)
                .WithMessage("Maximum price must be 0 or greater.");

            RuleFor(x => x)
                .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
                .WithMessage("Minimum price must be less than or equal to maximum price.");
        }
    }
}
