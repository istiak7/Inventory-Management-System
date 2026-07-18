using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierById
{
    public class GetSupplierByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-suppliers/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSupplierByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Supplier");
        }
    }
}
