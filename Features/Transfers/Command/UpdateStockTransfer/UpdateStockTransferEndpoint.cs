using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.UpdateStockTransfer
{
    public class UpdateStockTransferEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-stock-transfer/{id:int}", async (int id, UpdateStockTransferCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Transfers").RequirePermission(Permissions.TransfersManage);
        }
    }
}
