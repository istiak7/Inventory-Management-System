using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.ResolveWarrantyClaim
{
    public class ResolveWarrantyClaimEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/resolve-warranty-claim/{id:int}", async (
                int id, ResolveWarrantyClaimCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command with { WarrantyClaimId = id });
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyManage);
        }
    }
}
