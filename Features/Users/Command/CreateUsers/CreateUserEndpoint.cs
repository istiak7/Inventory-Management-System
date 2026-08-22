using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Command.CreateUsers
{
    public class CreateUserEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-user", async (CreateUserCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            })
            .WithTags("Users")
            .RequireAuthorization("AdminOnly");
        }
    }
}
