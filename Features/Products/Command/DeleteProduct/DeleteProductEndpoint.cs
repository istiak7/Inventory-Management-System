using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.DeleteProduct
{
    public class DeleteProductEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-products/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteProductCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("Product").RequirePermission(Permissions.ProductsManage);
        }
    }
}
