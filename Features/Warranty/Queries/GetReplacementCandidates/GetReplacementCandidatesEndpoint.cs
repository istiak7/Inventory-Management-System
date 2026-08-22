using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetReplacementCandidates
{
    public class GetReplacementCandidatesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-replacement-candidates/{claimId:int}", async (
                int claimId, IMediator mediator, string? search = null) =>
            {
                var result = await mediator.Send(new GetReplacementCandidatesQuery(claimId, search));
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyView);
        }
    }
}
