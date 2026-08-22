using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Command.UpdateSupplier
{
    public class UpdateSupplierEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-suppliers/{id:int}", async (int id, UpdateSupplierCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Supplier").RequirePermission(Permissions.SuppliersManage);
        }
    }
}
