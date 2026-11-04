using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Queries.GetModules
{
    public class GetModulesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapGet("/get-all-modules", async (IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetModulesQuery())))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
