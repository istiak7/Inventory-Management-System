using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductById
{
    public class GetProductByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-products/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetProductByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Product").RequirePermission(Permissions.ProductsView);
        }
    }
}
