using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Orders.Command.CreateOrder
{
    public class CreateOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-order", async (CreateOrderCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Order");
        }
    }
}
