using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Queries.GetMenuActions
{
    public class GetMenuActionsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapGet("/get-menu-actions", async (int menuId, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetMenuActionsQuery(menuId))))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
