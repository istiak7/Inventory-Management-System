using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Users.Command.ChangePassword
{
    // Any logged-in user can change their own password (and only their own).
    public class ChangePasswordEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/change-password", async (ChangePasswordCommand command, ICurrentUser currentUser, IMediator mediator) =>
            {
                if (currentUser.UserId is null)
                {
                    return Results.Unauthorized();
                }

                command.UserId = currentUser.UserId.Value;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            })
            .WithTags("Users")
            .RequireAuthorization();
        }
    }
}
