using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Queries.GetAllStockTransfers
{
    public class GetAllStockTransfersEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-stock-transfers", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? sourceBranchId = null,
                int? destinationBranchId = null,
                string? status = null,
                string? search = null) =>
            {
                var result = await mediator.Send(new GetAllStockTransfersQuery(
                    pageNumber, pageSize, sourceBranchId, destinationBranchId, status, search));
                return Results.Ok(result);
            }).WithTags("Transfers");
        }
    }
}
