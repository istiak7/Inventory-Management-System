using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerPayments
{
    public class GetCustomerPaymentsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-customer-payments", async (IMediator mediator, int customerId) =>
            {
                var result = await mediator.Send(new GetCustomerPaymentsQuery(customerId));
                return Results.Ok(result);
            }).WithTags("Customer").RequirePermission(Permissions.CustomersView);
        }
    }
}
