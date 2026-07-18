using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.UpdateProduct
{
    public class UpdateProductEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-products/{id:int}", async (int id, UpdateProductCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Product");
        }
    }
}
