using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Command.SaveMenu
{
    public class SaveMenuEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapPost("/create-menu", async (SaveMenuCommand command, IMediator mediator) =>
                Results.Ok(await mediator.Send(command)))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");

            app.MapPut("/update-menu/{id:int}", async (int id, SaveMenuCommand command, IMediator mediator) =>
            {
                command.Id = id;
                return Results.Ok(await mediator.Send(command));
            }).WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
