using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetAllStock
{
    public class GetAllStockEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-stock", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? branchId = null,
                int? productVariantId = null,
                string? search = null,
                int? lowStockThreshold = null) =>
            {
                var result = await mediator.Send(new GetAllStockQuery(pageNumber, pageSize, branchId, productVariantId, search, lowStockThreshold));
                return Results.Ok(result);
            }).WithTags("Stock").RequirePermission(Permissions.InventoryView);
        }
    }
}
