using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.DeleteWarrantyClaim
{
    public class DeleteWarrantyClaimEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-warranty-claim/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteWarrantyClaimCommand { WarrantyClaimId = id });
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyManage);
        }
    }
}
