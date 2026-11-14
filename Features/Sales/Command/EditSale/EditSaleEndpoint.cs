using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.EditSale
{
    public class EditSaleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Listed on the Sales page's "Edit Invoice" menu action (System Setup -> Menu Action):
            // Admin always, other roles only when granted. Without a menu action it falls back to
            // the fixed Sales Manage permission.
            app.MapPut("/edit-sale/{id:int}", async (int id, EditSaleCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Sales").RequirePermission(Permissions.SalesManage);
        }
    }
}
