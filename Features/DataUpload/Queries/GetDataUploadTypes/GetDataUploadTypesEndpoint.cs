using Inventory_Management_System.Shared;
using MediatR;
using System.Security.Claims;

namespace Inventory_Management_System.Features.DataUpload.Queries.GetDataUploadTypes
{
    public class GetDataUploadTypesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-data-upload-types", async (ClaimsPrincipal user, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetDataUploadTypesQuery(user));
                return Results.Ok(result);
            }).WithTags("DataUpload").RequireAuthorization();
        }
    }
}
