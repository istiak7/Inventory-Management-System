using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetMenuActionTree
{
    public class GetMenuActionTreeEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-menu-action-tree", async (IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetMenuActionTreeQuery())))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
