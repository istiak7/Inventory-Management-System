using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.DeleteStockTransfer
{
    public class DeleteStockTransferEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-stock-transfer/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteStockTransferCommand { StockTransferId = id });
                return Results.Ok(result);
            }).WithTags("Transfers").RequirePermission(Permissions.TransfersManage);
        }
    }
}
