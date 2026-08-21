using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.GetAllPurchaseOrders
{
    public class GetAllPurchaseOrdersEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-purchase-orders", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? status = null,
                int? supplierId = null,
                int? branchId = null,
                string? purchaseType = null,
                string? search = null,
                DateTime? startDate = null,
                DateTime? endDate = null) =>
            {
                var result = await mediator.Send(new GetAllPurchaseOrdersQuery(
                    pageNumber, pageSize, status, supplierId, branchId, purchaseType, search, startDate, endDate));
                return Results.Ok(result);
            }).WithTags("Purchase");
        }
    }
}
