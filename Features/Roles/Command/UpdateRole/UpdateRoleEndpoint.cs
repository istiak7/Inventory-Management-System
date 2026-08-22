using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Command.UpdateRole
{
    public class UpdateRoleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-role/{id:int}", async (int id, UpdateRoleCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            })
            .WithTags("Roles")
            .RequireAuthorization("AdminOnly");
        }
    }
}
