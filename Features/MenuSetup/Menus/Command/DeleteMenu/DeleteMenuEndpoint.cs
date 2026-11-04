using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Command.DeleteMenu
{
    public class DeleteMenuEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapDelete("/delete-menu/{id:int}", async (int id, IMediator mediator) =>
                Results.Ok(await mediator.Send(new DeleteMenuCommand(id))))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
