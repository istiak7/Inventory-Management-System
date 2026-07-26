using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.ReceiveGoods
{
    public class ReceiveGoodsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/receive-purchase-order/{id:int}", async (int id, ReceiveGoodsCommand command, IMediator mediator) =>
            {
                command.PurchaseOrderId = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchase");
        }
    }
}
