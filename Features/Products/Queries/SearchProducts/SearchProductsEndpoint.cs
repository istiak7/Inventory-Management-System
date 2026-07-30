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
                bool? isSerialized = null,
                decimal? minPrice = null,
                decimal? maxPrice = null,
                int pageNumber = 1,
                int pageSize = 20) =>
            {
                var result = await mediator.Send(new SearchProductsQuery(
                    term, productId, isSerialized, minPrice, maxPrice, pageNumber, pageSize));

                return Results.Ok(result);
            })
            .WithTags("Product")
            .WithSummary("Full-text search over product variants (name, SKU, barcode, attributes).");
        }
    }
}
