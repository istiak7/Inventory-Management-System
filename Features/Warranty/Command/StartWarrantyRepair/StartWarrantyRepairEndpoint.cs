using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.StartWarrantyRepair
{
    public class StartWarrantyRepairEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/start-warranty-repair/{id:int}", async (
                int id, StartWarrantyRepairCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command with { WarrantyClaimId = id });
                return Results.Ok(result);
            }).WithTags("Warranty");
        }
    }
}
