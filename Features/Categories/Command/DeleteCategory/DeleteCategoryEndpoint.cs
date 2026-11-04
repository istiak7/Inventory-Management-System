using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.DeleteCategory
{
    public class DeleteCategoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-categories/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteCategoryCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("Category").RequirePermission(Permissions.ProductsManage);
        }
    }
}
