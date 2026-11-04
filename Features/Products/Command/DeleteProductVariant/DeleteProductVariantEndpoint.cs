using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Command.DeleteProductVariant
{
    public class DeleteProductVariantEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-product-variants/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteProductVariantCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("Product").RequirePermission(Permissions.ProductsManage);
        }
    }
}
