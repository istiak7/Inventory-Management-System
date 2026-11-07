using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductFilterOptions
{
    public class GetProductFilterOptionsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-product-filter-options", async (
                IMediator mediator,
                int? subCategoryId = null,
                int? brandId = null,
                bool? isSerialized = null) =>
            {
                var result = await mediator.Send(new GetProductFilterOptionsQuery(subCategoryId, brandId, isSerialized));
                return Results.Ok(result);
            })
            .WithTags("Product")
            .WithSummary("Sub-categories and brands that have product variants, each narrowed by the other filters.")
            .RequirePermission(Permissions.ProductsView);
        }
    }
}
