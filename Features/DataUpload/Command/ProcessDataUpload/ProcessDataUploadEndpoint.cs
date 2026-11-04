using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory_Management_System.Features.DataUpload.Command.ProcessDataUpload
{
    public class ProcessDataUploadEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/process-data-upload", async (
                [FromForm] ProcessDataUploadCommand command,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                // Each type needs the same permission as its own Create screen.
                var definition = DataUploadCatalog.Find(command.Type);
                if (definition is not null && !DataUploadCatalog.CanUpload(user, definition))
                    return DataUploadCatalog.Forbidden(definition);

                var result = await mediator.Send(command, cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("DataUpload")
            .RequireAuthorization()
            // The API uses bearer tokens, not cookies, so a form post cannot be forged from another site.
            .DisableAntiforgery();
        }
    }
}
