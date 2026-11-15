using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Queries.GetAllUnitsOfMeasure
{
    public class GetAllUnitsOfMeasureEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-units-of-measure", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? search = null,
                string? status = null) =>
            {
                var result = await mediator.Send(new GetAllUnitsOfMeasureQuery(pageNumber, pageSize, search, status));
                return Results.Ok(result);
            }).WithTags("UnitOfMeasure").RequirePermission(Permissions.ProductsView);
        }
    }
}
