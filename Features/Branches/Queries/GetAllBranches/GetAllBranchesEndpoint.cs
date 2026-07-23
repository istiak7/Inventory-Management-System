using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Queries.GetAllBranches
{
    public class GetAllBranchesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-branches", async (IMediator mediator, int pageNumber = 1, int pageSize = 100) =>
            {
                var result = await mediator.Send(new GetAllBranchesQuery(pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("Branch");
        }
    }
}
