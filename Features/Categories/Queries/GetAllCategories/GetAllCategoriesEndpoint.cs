using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetAllCategories
{
    public class GetAllCategoriesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-categories", async (IMediator mediator, int pageNumber = 1, int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetAllCategoriesQuery(pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("Category").RequirePermission(Permissions.ProductsView);
        }
    }
}
