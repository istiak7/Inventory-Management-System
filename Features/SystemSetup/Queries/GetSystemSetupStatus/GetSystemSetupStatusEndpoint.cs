using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SystemSetup.Queries.GetSystemSetupStatus
{
    public class GetSystemSetupStatusEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Every logged-in user gets the "setup incomplete" warning, so every user may read the status.
            // It returns counts only (plus the letterhead, which every user already prints).
            app.MapGet("/get-system-setup-status", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSystemSetupStatusQuery());
                return Results.Ok(result);
            }).WithTags("SystemSetup").RequireAuthorization();
        }
    }
}
