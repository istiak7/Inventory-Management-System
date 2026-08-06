using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.LookupSerialWarranty
{
    public class LookupSerialWarrantyEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // The serial travels as a query string, not a route segment: serial numbers routinely
            // carry '/' and '.', which would otherwise split the path.
            app.MapGet("/lookup-warranty-serial", async (IMediator mediator, string serialNumber) =>
            {
                var result = await mediator.Send(new LookupSerialWarrantyQuery(serialNumber));
                return Results.Ok(result);
            }).WithTags("Warranty");
        }
    }
}
