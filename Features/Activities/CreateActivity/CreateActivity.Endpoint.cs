using MediatR;
using Inventory_Management_System.Shared;

namespace Inventory_Management_System.Features.Activities.CreateActivity
{
   public class CreateActivityEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/activities", async (CreateActivityCommand command, IMediator mediator) =>
            {

                var result = await mediator.Send(command);
                return Results.Ok(result);

            });
        }
    }
}
