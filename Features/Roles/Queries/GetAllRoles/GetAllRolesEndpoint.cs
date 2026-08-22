using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Queries.GetAllRoles
{
    public class GetAllRolesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-roles", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAllRolesQuery());
                return Results.Ok(result);
            })
            .WithTags("Roles")
            .RequireAuthorization("AdminOnly");
        }
    }
}
