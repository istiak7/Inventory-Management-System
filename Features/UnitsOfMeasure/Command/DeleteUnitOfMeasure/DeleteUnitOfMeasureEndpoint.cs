using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.DeleteUnitOfMeasure
{
    public class DeleteUnitOfMeasureEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-units-of-measure/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteUnitOfMeasureCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("UnitOfMeasure").RequirePermission(Permissions.ProductsManage);
        }
    }
}
