using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Command.UpdateCustomer
{
    public class UpdateCustomerEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/update-customers/{id:int}", async (int id, UpdateCustomerCommand command, IMediator mediator) =>
            {
                command.Id = id;
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }).WithTags("Customer").RequirePermission(Permissions.CustomersManage);
        }
    }
}
