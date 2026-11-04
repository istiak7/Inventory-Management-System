using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Branches.Command.UpdateBranch
{
    public class UpdateBranchEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-branches/{id:int}", async (int id, UpdateBranchCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Branch").RequirePermission(Permissions.BranchesManage);
        }
    }
}
