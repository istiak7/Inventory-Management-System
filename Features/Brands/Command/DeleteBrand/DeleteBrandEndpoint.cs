using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Brands.Command.DeleteBrand
{
    public class DeleteBrandEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-brands/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteBrandCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("Brand").RequirePermission(Permissions.ProductsManage);
        }
    }
}
