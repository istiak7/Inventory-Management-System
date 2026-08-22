using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductVariants
{
    public class GetProductVariantsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-product-variants", async (IMediator mediator, int? productId = null) =>
            {
                var result = await mediator.Send(new GetProductVariantsQuery(productId));
                return Results.Ok(result);
            }).WithTags("Product").RequirePermission(Permissions.ProductsView);
        }
    }
}
