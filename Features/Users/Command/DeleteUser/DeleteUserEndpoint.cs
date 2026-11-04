using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Command.DeleteUser
{
    public class DeleteUserEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-user/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteUserCommand { Id = id });
                return Results.Ok(result);
            })
            .WithTags("Users")
            .RequireAuthorization("AdminOnly");
        }
    }
}
