using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.SystemSetup.Command.SaveCompanySetting
{
    public class SaveCompanySettingEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/save-company-setting", async (SaveCompanySettingCommand command, IMediator mediator) =>
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            })
            .WithTags("SystemSetup")
            // Company identity is installation-wide, like users and roles: admins only.
            .RequireAuthorization("AdminOnly");
        }
    }
}
