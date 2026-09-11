using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.DeletePurchaseOrder
{
    public class DeletePurchaseOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-purchase-order/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeletePurchaseOrderCommand { PurchaseOrderId = id });
                return Results.Ok(result);
            }).WithTags("Purchase").RequirePermission(Permissions.PurchasesManage);
        }
    }
}
