using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetSubCategoryByCategoryId
{
    public class GetSubCategoryByCategoryIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/categories/{categoryId:int}/sub-categories", async (int categoryId, IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetSubCategoryByCategoryIdQuery(categoryId, pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("Category");
        }
    }
}
