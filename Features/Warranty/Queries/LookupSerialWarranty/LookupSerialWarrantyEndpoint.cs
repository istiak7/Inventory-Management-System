using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.LookupSerialWarranty
{
    public class LookupSerialWarrantyEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/lookup-warranty-serial", async (IMediator mediator, string serialNumber) =>
            {
                var result = await mediator.Send(new LookupSerialWarrantyQuery(serialNumber));
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyView);
        }
    }
}
