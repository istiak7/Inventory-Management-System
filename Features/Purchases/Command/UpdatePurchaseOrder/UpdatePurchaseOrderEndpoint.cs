using Inventory_Management_System.Shared;
using MediatR;
using Inventory_Management_System.Shared.CurrentUser;

namespace Inventory_Management_System.Features.Purchases.Command.UpdatePurchaseOrder
{
    public class UpdatePurchaseOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Same reach as creating one: a member can correct an order they raised, but only
            // while it is still pending. The handler refuses anything already approved.
            app.MapPut("/update-purchase-order/{id:int}", async (int id, UpdatePurchaseOrderCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchase").RequirePermission(Permissions.PurchasesManage);
        }
    }
}
