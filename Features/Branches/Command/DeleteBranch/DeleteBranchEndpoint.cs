using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Command.DeleteBranch
{
    public class DeleteBranchEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-branches/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteBranchCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("Branch").RequirePermission(Permissions.BranchesManage);
        }
    }
}
