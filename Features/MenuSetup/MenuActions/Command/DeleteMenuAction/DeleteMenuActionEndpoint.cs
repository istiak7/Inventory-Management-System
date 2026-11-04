using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Command.DeleteMenuAction
{
    public class DeleteMenuActionEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapDelete("/delete-menu-action/{id:int}", async (int id, IMediator mediator) =>
                Results.Ok(await mediator.Send(new DeleteMenuActionCommand(id))))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
