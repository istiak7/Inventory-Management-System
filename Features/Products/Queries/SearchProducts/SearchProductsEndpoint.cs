using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.SearchProducts
{
    public class SearchProductsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/search-products", async (
                IMediator mediator,
                string? term = null,
                int? productId = null,
                int? variantId = null,
                int? subCategoryId = null,
                int? brandId = null,
                int? branchId = null,
                bool? isSerialized = null,
                int? subCategoryId = null,
                int? brandId = null,
                decimal? minPrice = null,
                decimal? maxPrice = null,
                int pageNumber = 1,
                int pageSize = 20) =>
            {
                var result = await mediator.Send(new SearchProductsQuery(
                    Term: term,
                    ProductId: productId,
                    VariantId: variantId,
                    SubCategoryId: subCategoryId,
                    BrandId: brandId,
                    BranchId: branchId,
                    IsSerialized: isSerialized,
                    MinPrice: minPrice,
                    MaxPrice: maxPrice,
                    PageNumber: pageNumber,
                    PageSize: pageSize));

                return Results.Ok(result);
            })
            .WithTags("Product")
            .WithSummary("Full-text search over product variants (name, SKU, barcode, attributes). " +
                         "Pass branchId to scope results to variants stocked in that branch.")
            .RequirePermission(Permissions.ProductsView);
        }
    }
}
