using Inventory_Management_System.Features.Users.Queries.GetUserById;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Users.Queries.GetCurrentUser
{
    // Returns the logged-in user with their role, branch and effective permissions.
    // Any authenticated user can call this (it only ever returns their own record).
    public class GetCurrentUserEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/me", async (ICurrentUser currentUser, IMediator mediator) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Unauthorized();
                }

                var result = await mediator.Send(new GetUserByIdQuery(currentUser.UserId.Value));
                return Results.Ok(result);
            })
            .WithTags("Users")
            .RequireAuthorization();
        }
    }
}
