using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.DeleteCustomer
{
    public class DeleteCustomerEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/delete-customers/{id:int}", async (int id, IMediator mediator) =>
            {
                var result = await mediator.Send(new DeleteCustomerCommand { Id = id });
                return Results.Ok(result);
            }).WithTags("Customer").RequirePermission(Permissions.CustomersManage);
        }
    }
}
