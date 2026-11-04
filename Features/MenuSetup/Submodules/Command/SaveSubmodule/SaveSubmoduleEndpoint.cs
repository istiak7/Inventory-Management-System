using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Command.SaveSubmodule
{
    public class SaveSubmoduleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapPost("/create-submodule", async (SaveSubmoduleCommand command, IMediator mediator) =>
                Results.Ok(await mediator.Send(command)))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");

            app.MapPut("/update-submodule/{id:int}", async (int id, SaveSubmoduleCommand command, IMediator mediator) =>
            {
                command.Id = id;
                return Results.Ok(await mediator.Send(command));
            }).WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
