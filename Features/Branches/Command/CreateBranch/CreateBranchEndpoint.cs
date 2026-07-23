using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Command.CreateBranch
{
    public class CreateBranchEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/create-branches", async (CreateBranchCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Branch");
        }
    }
}
