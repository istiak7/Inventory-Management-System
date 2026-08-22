using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetAllSubCategories
{
    public class GetAllSubCategoriesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-all-sub-categories", async (IMediator mediator, int pageNumber = 1, int pageSize = 20) =>
            {
                var result = await mediator.Send(new GetAllSubCategoriesQuery(pageNumber, pageSize));
                return Results.Ok(result);
            }).WithTags("SubCategory").RequirePermission(Permissions.ProductsView);
        }
    }
}
