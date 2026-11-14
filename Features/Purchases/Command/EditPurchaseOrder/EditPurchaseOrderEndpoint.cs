using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.EditPurchaseOrder
{
    public class EditPurchaseOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Listed on the Purchases page's "Edit Approved Purchase" menu action (System Setup ->
            // Menu Action): Admin always, other roles only when granted. Without a menu action it
            // falls back to the fixed Purchases Manage permission.
            app.MapPut("/edit-purchase-order/{id:int}", async (int id, EditPurchaseOrderCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchases").RequirePermission(Permissions.PurchasesManage);
        }
    }
}
