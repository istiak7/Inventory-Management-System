using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Command.CancelSale
{
    public class CancelSaleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Listed on the Sales page's "Delete Invoice" menu action (System Setup -> Menu Action):
            // Admin always, other roles only when granted. Without a menu action it falls back to
            // the fixed Sales Manage permission.
            app.MapPost("/cancel-sale/{id:int}", async (int id, CancelSaleCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Sales").RequirePermission(Permissions.SalesManage);
        }
    }
}
