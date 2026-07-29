using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Command.CreatePurchaseOrder
{
    public class CreatePurchaseOrderEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-purchase-order", async (CreatePurchaseOrderRequest request, IMediator mediator) =>
            {
                var command = new CreatePurchaseOrderCommand
                {
                    SupplierId = request.SupplierId,
                    BranchId = request.BranchId,
                    PurchaseDate = request.PurchaseDate,
                    InvoiceNumber = request.InvoiceNumber,
                    Items = request.Items
                };
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Purchase");
        }
    }
}
