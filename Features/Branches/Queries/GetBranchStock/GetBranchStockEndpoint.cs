using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetBranchStock
{
    public class GetBranchStockEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-branch-stock/{branchId:int}", async (int branchId, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetBranchStockQuery(branchId));
                return Results.Ok(result);
            }).WithTags("Branch").RequirePermission(Permissions.InventoryView);
        }
    }
}
