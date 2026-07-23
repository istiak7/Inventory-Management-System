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
                int? supplierId = null) =>
            {
                var result = await mediator.Send(new GetAllPurchaseOrdersQuery(pageNumber, pageSize, status, supplierId));
                return Results.Ok(result);
            }).WithTags("Purchase");
        }
    }
}
