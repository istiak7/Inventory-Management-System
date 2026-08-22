using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/categories/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new GetCategoryByIdQuery(id));
                return Results.Ok(result);
            }).WithTags("Category").RequirePermission(Permissions.ProductsView);
        }
    }
}
