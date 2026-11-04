using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Queries.GetMenus
{
    public class GetMenusEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapGet("/get-menus", async (int submoduleId, IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetMenusQuery(submoduleId))))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
