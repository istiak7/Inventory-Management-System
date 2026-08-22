using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetSubCategoryById
{
    public class GetSubCategoryByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/sub-categories/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetSubCategoryByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("SubCategory").RequirePermission(Permissions.ProductsView);
        }
    }
}
