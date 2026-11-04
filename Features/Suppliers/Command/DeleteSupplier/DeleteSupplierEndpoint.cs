using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Command.DeleteSupplier
{
    public class DeleteSupplierEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-suppliers/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteSupplierCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("Supplier").RequirePermission(Permissions.SuppliersManage);
        }
    }
}
