using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Login
{
    public class LoginEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/login", async (LoginUserCommand request, IMediator mediator) =>
            {
                var response = await mediator.Send(request);
                return Results.Ok(response);
            }).WithTags("Authentication");
        }
    }
}
