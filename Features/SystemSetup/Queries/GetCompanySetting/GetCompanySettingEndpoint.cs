using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SystemSetup.Queries.GetCompanySetting
{
    public class GetCompanySettingEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Every logged-in user prints invoices, so every logged-in user may read the letterhead.
            app.MapGet("/get-company-setting", async (IMediator mediator) =>
            {
                var result = await mediator.Send(new GetCompanySettingQuery());
                return Results.Ok(result);
            }).WithTags("SystemSetup").RequireAuthorization();
        }
    }
}
