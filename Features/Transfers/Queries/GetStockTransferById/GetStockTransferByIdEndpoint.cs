using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Queries.GetStockTransferById
{
    public class GetStockTransferByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-stock-transfer-by-id/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetStockTransferByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Transfers");
        }
    }
}
