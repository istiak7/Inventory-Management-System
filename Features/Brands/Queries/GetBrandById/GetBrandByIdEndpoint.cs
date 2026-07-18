using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetBrandById
{
    public class GetBrandByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-brands/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetBrandByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Brand");
        }
    }
}
