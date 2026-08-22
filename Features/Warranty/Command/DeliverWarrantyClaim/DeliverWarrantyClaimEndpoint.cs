using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.DeliverWarrantyClaim
{
    public class DeliverWarrantyClaimEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/deliver-warranty-claim/{id:int}", async (
                int id, DeliverWarrantyClaimCommand? command, IMediator mediator) =>
            {
                var result = await mediator.Send((command ?? new DeliverWarrantyClaimCommand()) with { WarrantyClaimId = id });
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyManage);
        }
    }
}
