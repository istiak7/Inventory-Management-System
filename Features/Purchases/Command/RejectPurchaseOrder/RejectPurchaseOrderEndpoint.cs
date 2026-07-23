using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.RejectPurchaseOrder
{
    public class RejectPurchaseOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/reject-purchase-order/{id}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new RejectPurchaseOrderCommand { PurchaseOrderId = id });
                return Results.Ok(result);
            }).WithTags("Purchase");
        }
    }
}
