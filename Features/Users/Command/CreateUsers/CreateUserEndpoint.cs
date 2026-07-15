using MediatR;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Users.Command.CreateUsers
{
    public class CreateUserEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/Registration", async (CreateUserCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            });
        }
    }
}
