using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.RejectWarrantyClaim
{
    public class RejectWarrantyClaimEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/reject-warranty-claim/{id:int}", async (
                int id, RejectWarrantyClaimCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command with { WarrantyClaimId = id });
                return Results.Ok(result);
            }).WithTags("Warranty");
        }
    }
}
