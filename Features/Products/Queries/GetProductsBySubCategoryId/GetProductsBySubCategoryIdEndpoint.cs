using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.GetProductsBySubCategoryId
{
    public class GetProductsBySubCategoryIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/sub-categories/{subCategoryId:int}/products", async (int subCategoryId, IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetProductsBySubCategoryIdQuery(subCategoryId, pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("SubCategory");
        }
    }
}
