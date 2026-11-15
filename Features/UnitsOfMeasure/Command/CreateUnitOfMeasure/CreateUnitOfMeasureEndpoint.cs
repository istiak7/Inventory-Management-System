using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.CreateUnitOfMeasure
{
    public class CreateUnitOfMeasureEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-units-of-measure", async (CreateUnitOfMeasureCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("UnitOfMeasure").RequirePermission(Permissions.ProductsManage);
        }
    }
}
