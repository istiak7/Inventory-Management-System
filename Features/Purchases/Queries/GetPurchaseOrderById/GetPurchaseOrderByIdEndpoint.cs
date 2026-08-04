using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.GetPurchaseOrderById
{
    public class GetPurchaseOrderByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-purchase-order/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetPurchaseOrderByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Purchase");
        }
    }
}
