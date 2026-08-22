using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Users.Queries.GetUserById
{
    public class GetUserByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-user/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetUserByIdQuery(id));
                return Results.Ok(result);
            })
            .WithTags("Users")
            .RequireAuthorization("AdminOnly");
        }
    }
}
