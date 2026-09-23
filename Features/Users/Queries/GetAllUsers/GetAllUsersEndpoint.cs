using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-users", async (IMediator mediator, int pageNumber = 1, int pageSize = 10, string? search = null) =>
            {
                var result = await mediator.Send(new GetAllUsersQuery(pageNumber, pageSize, search));
                return Results.Ok(result);
            })
            .WithTags("Users")
            .RequireAuthorization("AdminOnly");
        }
    }
}
