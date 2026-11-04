using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Command.DeleteSubmodule
{
    public class DeleteSubmoduleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapDelete("/delete-submodule/{id:int}", async (int id, IMediator mediator) =>
                Results.Ok(await mediator.Send(new DeleteSubmoduleCommand(id))))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
