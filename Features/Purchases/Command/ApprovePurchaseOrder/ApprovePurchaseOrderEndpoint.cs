using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.ApprovePurchaseOrder
{
    public class ApprovePurchaseOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/approve-purchase-order/{id}", async (int id, ApprovePurchaseOrderRequest request, IMediator mediator) =>
            {
                var command = new ApprovePurchaseOrderCommand
                {
                    PurchaseOrderId = id,
                    Payment = request.Payment
                };
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchase");
        }
    }
}
