using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.CreateWarrantyClaim
{
    public class CreateWarrantyClaimEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-warranty-claim", async (CreateWarrantyClaimCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyManage);
        }
    }
}
