using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetProductsByBrandId
{
    public class GetProductsByBrandIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-brands/{brandId:int}/products", async (int brandId, IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetProductsByBrandIdQuery(brandId, pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("Brand");
        }
    }
}
