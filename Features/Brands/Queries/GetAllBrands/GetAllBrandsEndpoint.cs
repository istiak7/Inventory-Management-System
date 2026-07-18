using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Queries.GetAllBrands
{
    public class GetAllBrandsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-brands", async (IMediator mediator, int pageNumber = 1, int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetAllBrandsQuery(pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("Brand");
        }
    }
}
