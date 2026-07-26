using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetBranchVariantStock
{
    public class GetBranchVariantStockEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-branch-stock/{branchId:int}/variant/{productVariantId:int}", async (
                int branchId, int productVariantId, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetBranchVariantStockQuery(branchId, productVariantId));
                return Results.Ok(result);
            }).WithTags("Branch");
        }
    }
}
