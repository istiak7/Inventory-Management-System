using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-products", async (IMediator mediator, int pageNumber = 1, int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetAllProductsQuery(pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("Product");
        }
    }
}
