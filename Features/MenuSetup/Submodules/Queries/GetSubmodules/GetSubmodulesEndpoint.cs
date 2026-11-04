using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Queries.GetSubmodules
{
    public class GetSubmodulesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {

            app.MapGet("/get-all-submodules", async (IMediator mediator) =>
                Results.Ok(await mediator.Send(new GetSubmodulesQuery())))
               .WithTags("SystemSetup").RequireAuthorization("AdminOnly");
        }
    }
}
