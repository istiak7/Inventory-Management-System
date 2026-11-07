using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductFilterOptions
{
    // The sub-categories and brands that actually have product variants, for the filter
    // dropdowns next to a product picker. Each list is narrowed by the OTHER filters, so
    // choosing a brand only leaves sub-categories that brand has products in, and the other
    // way round.
    public sealed record GetProductFilterOptionsQuery(
        int? SubCategoryId = null,
        int? BrandId = null,
        bool? IsSerialized = null
    ) : IRequest<Result>;

    public sealed record SubCategoryFilterOption(int Id, string Name, string CategoryName, int VariantCount);

    public sealed record BrandFilterOption(int Id, string Name, int VariantCount);

    public sealed record GetProductFilterOptionsResponse(
        IReadOnlyList<SubCategoryFilterOption> SubCategories,
        IReadOnlyList<BrandFilterOption> Brands
    );
}
