using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.CurrentUser;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.CheckSerialNumbers
{
    public class CheckSerialNumbersEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // POST so a long list of serials does not end up in the URL. Same permission as
            // raising a purchase order, which is the only screen that uses it.
            app.MapPost("/check-serial-numbers", async (CheckSerialNumbersRequest request, IMediator mediator) =>
            {
                var result = await mediator.Send(new CheckSerialNumbersQuery(
                    request.SerialNumbers ?? [], request.ExcludePurchaseOrderId));
                return Results.Ok(result);
            }).WithTags("Purchase").RequirePermission(Permissions.PurchasesManage);
        }
    }
}
