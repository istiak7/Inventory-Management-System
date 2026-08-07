using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetWarrantyClaimById
{
    public class GetWarrantyClaimByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-warranty-claim/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetWarrantyClaimByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Warranty");
        }
    }
}
