using Inventory_Management_System.Features.Transfers.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Command.CreateStockTransfer
{
    public class CreateStockTransferEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-stock-transfer", async (CreateStockTransferRequest request, IMediator mediator) =>
            {
                var command = new CreateStockTransferCommand
                {
                    SourceBranchId = request.SourceBranchId,
                    DestinationBranchId = request.DestinationBranchId,
                    Items = request.Items,
                    Notes = request.Notes,
                    Dispatch = request.Dispatch,
                };
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Transfers");
        }
    }
}
