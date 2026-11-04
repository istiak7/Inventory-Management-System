using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Command.SaveMenuAction
{
    public class SaveMenuActionEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapPost("/create-menu-action", async (SaveMenuActionCommand command, IMediator mediator) =>
                Results.Ok(await mediator.Send(command)))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");

            app.MapPut("/update-menu-action/{id:int}", async (int id, SaveMenuActionCommand command, IMediator mediator) =>
            {
                command.Id = id;
                return Results.Ok(await mediator.Send(command));
            }).WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
