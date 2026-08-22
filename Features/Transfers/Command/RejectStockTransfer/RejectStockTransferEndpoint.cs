using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.RejectStockTransfer
{
    public class RejectStockTransferEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/reject-stock-transfer/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new RejectStockTransferCommand { StockTransferId = id });
                return Results.Ok(result);
            }).WithTags("Transfers").RequirePermission(Permissions.TransfersManage);
        }
    }
}
