using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.ApproveStockTransfer
{
    public class ApproveStockTransferEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/approve-stock-transfer/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new ApproveStockTransferCommand { StockTransferId = id });
                return Results.Ok(result);
            }).WithTags("Transfers");
        }
    }
}
