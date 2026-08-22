using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariantAndBranch
{
    public class GetStockByVariantAndBranchEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-stock-by-variant-and-branch", async (
                IMediator mediator,
                int productVariantId,
                int branchId) =>
            {
                var result = await mediator.Send(new GetStockByVariantAndBranchQuery(productVariantId, branchId));
                return Results.Ok(result);
            }).WithTags("Stock").RequirePermission(Permissions.InventoryView);
        }
    }
}
