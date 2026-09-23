using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Users.Login
{
    public class LoginEndpoint : IEndpoint
    {
        // Name of the rate limit policy (set up in Program.cs) that slows down password guessing.
        public const string AuthRateLimitPolicy = "auth";

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/login", async (LoginUserCommand request, IMediator mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            }).WithTags("Authentication").RequireRateLimiting(AuthRateLimitPolicy);

            // No access token needed: the refresh token itself is the proof.
            app.MapPost("/refresh-token", async (RefreshTokenCommand request, IMediator mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            }).WithTags("Authentication").RequireRateLimiting(AuthRateLimitPolicy);

            app.MapPost("/logout", async (LogoutCommand? command, ICurrentUser currentUser, IMediator mediator) =>
            {
                if (currentUser.UserId is null)
                    return Results.Unauthorized();

                command ??= new LogoutCommand();
                command.UserId = currentUser.UserId.Value;
                var response = await mediator.Send(command);
                return Results.Ok(response);
            }).WithTags("Authentication").RequireAuthorization();
        }
    }
}
