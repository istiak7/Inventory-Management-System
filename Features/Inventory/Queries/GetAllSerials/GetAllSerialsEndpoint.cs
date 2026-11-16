using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Inventory.Queries.GetAllSerials
{
    public class GetAllSerialsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-serials", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? branchId = null,
                int? productVariantId = null,
                string? status = null,
                string? search = null) =>
            {
                var result = await mediator.Send(new GetAllSerialsQuery(pageNumber, pageSize, branchId, productVariantId, status, search));
                return Results.Ok(result);
            }).WithTags("Inventory").RequirePermission(Permissions.InventoryView);
        }
    }
}
