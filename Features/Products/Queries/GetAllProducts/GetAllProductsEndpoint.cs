using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-products", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                string? search = null,
                int? subCategoryId = null,
                int? brandId = null) =>
            {
                var result = await mediator.Send(
                    new GetAllProductsQuery(pageNumber, pageSize, search, subCategoryId, brandId));
                return Results.Ok(result);
            }).WithTags("Product").RequirePermission(Permissions.ProductsView);
        }
    }
}
