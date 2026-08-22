using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Roles.Queries.GetAllPermissions
{
    public class GetAllPermissionsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-permissions", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetAllPermissionsQuery());
                return Results.Ok(result);
            })
            .WithTags("Roles")
            .RequireAuthorization("AdminOnly");
        }
    }
}
