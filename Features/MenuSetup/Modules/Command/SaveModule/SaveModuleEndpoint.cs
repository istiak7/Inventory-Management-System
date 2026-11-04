using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Command.SaveModule
{
    public class SaveModuleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapPost("/create-module", async (SaveModuleCommand command, IMediator mediator) =>
                Results.Ok(await mediator.Send(command)))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");

            app.MapPut("/update-module/{id:int}", async (int id, SaveModuleCommand command, IMediator mediator) =>
            {
                command.Id = id;
                return Results.Ok(await mediator.Send(command));
            }).WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
