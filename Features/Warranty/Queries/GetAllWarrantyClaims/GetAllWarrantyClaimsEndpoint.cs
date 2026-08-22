using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetAllWarrantyClaims
{
    public class GetAllWarrantyClaimsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-warranty-claims", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? status = null,
                int? branchId = null,
                int? customerId = null,
                string? search = null) =>
            {
                var result = await mediator.Send(
                    new GetAllWarrantyClaimsQuery(pageNumber, pageSize, status, branchId, customerId, search));
                return Results.Ok(result);
            }).WithTags("Warranty").RequirePermission(Permissions.WarrantyView);
        }
    }
}
