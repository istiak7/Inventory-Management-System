using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariant
{
    public class GetStockByVariantEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-stock-by-variant/{variantId:int}", async (int variantId, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetStockByVariantQuery(variantId));
                return Results.Ok(result);
            }).WithTags("Stock").RequirePermission(Permissions.InventoryView);
        }
    }
}
