using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerAccounts
{
    public class GetCustomerAccountsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-customer-accounts", async (IMediator mediator, int? customerId = null) =>
            {
                var result = await mediator.Send(new GetCustomerAccountsQuery(customerId));
                return Results.Ok(result);
            }).WithTags("Customer");
        }
    }
}
