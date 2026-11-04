using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetNavigation
{
    public class GetNavigationEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Every logged-in user needs the menus; each entry carries the permission that
            // decides whether this user sees it.
            app.MapGet("/get-navigation", async (IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetNavigationQuery())))
               .WithTags("SystemSetup").RequireAuthorization();
        }
    }
}
