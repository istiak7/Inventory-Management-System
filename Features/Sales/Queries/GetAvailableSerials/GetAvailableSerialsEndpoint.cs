using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetAvailableSerials
{
    public class GetAvailableSerialsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-available-serials", async (
                IMediator mediator,
                int productVariantId,
                int branchId,
                string? search = null) =>
            {
                var result = await mediator.Send(
                    new GetAvailableSerialsQuery(productVariantId, branchId, search));
                return Results.Ok(result);
            }).WithTags("Sales").RequirePermission(Permissions.SalesView);
        }
    }
}
