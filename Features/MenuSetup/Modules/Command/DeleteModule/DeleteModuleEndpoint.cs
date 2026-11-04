using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Command.DeleteModule
{
    public class DeleteModuleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapDelete("/delete-module/{id:int}", async (int id, IMediator mediator) =>
                Results.Ok(await mediator.Send(new DeleteModuleCommand(id))))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
