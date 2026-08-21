using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerByPhone
{
    public class GetCustomerByPhoneEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-customer-by-phone", async (
                IMediator mediator,
                string phoneNumber) =>
            {
                var result = await mediator.Send(new GetCustomerByPhoneQuery(phoneNumber));
                return Results.Ok(result);
            }).WithTags("Customer");
        }
    }
}
