using Inventory_Management_System.Features.DataUpload.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory_Management_System.Features.DataUpload.Queries.MatchDataUploadColumns
{
    public class MatchDataUploadColumnsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/match-data-upload-columns", async (
                [FromForm] MatchDataUploadColumnsQuery query,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var definition = DataUploadCatalog.Find(query.Type);
                if (definition is not null && !DataUploadCatalog.CanUpload(user, definition))
                    return DataUploadCatalog.Forbidden(definition);

                var result = await mediator.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("DataUpload")
            .RequireAuthorization()
            // The API uses bearer tokens, not cookies, so a form post cannot be forged from another site.
            .DisableAntiforgery();
        }
    }
}
