using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Command.UpdateUser
{
    public class UpdateUserEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-user/{id:int}", async (int id, UpdateUserCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            })
            .WithTags("Users")
            .RequireAuthorization("AdminOnly");
        }
    }
}
