using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.UpdateWarrantyClaim
{
    public class UpdateWarrantyClaimEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-warranty-claim/{id:int}", async (int id, UpdateWarrantyClaimCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyManage);
        }
    }
}
