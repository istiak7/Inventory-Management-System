using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Command.DeleteRole
{
    public class DeleteRoleEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-role/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteRoleCommand { Id = id });
                return Results.Ok(result);
            })
            .WithTags("Roles")
            .RequireAuthorization("AdminOnly");
        }
    }
}
