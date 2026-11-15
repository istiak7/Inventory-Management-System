using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Queries.GetUnitOfMeasureById
{
    public class GetUnitOfMeasureByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-units-of-measure/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetUnitOfMeasureByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("UnitOfMeasure").RequirePermission(Permissions.ProductsView);
        }
    }
}
