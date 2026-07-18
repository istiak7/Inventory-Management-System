using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetAllSuppliers
{
    public class GetAllSuppliersEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-suppliers", async (IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetAllSuppliersQuery(pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("Supplier");
        }
    }
}
