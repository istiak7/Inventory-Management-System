using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.UpdateUnitOfMeasure
{
    public class UpdateUnitOfMeasureEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-units-of-measure/{id:int}", async (int id, UpdateUnitOfMeasureCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("UnitOfMeasure").RequirePermission(Permissions.ProductsManage);
        }
    }
}
