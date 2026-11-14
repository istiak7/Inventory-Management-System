using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.CancelPurchaseOrder
{
    public class CancelPurchaseOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Listed on the Purchases page's "Delete Approved Purchase" menu action (System Setup ->
            // Menu Action): Admin always, other roles only when granted. Without a menu action it
            // falls back to the fixed Purchases Manage permission.
            app.MapPost("/cancel-purchase-order/{id:int}", async (int id, CancelPurchaseOrderCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchase").RequirePermission(Permissions.PurchasesManage);
        }
    }
}
