using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Categories.Command.DeleteSubCategory
{
    public class DeleteSubCategoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-sub-categories/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteSubCategoryCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("SubCategory").RequirePermission(Permissions.ProductsManage);
        }
    }
}
