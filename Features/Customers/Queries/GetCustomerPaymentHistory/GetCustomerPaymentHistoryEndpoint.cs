using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerPaymentHistory
{
    public class GetCustomerPaymentHistoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-customer-payment-history", async (
                IMediator mediator,
                int customerId,
                int pageNumber = 1,
                int pageSize = 10,
                string? invoiceNumber = null,
                int? paymentId = null,
                string? paymentMethod = null) =>
            {
                var result = await mediator.Send(
                    new GetCustomerPaymentHistoryQuery(customerId, pageNumber, pageSize, invoiceNumber, paymentId, paymentMethod));
                return Results.Ok(result);
            }).WithTags("Customer");
        }
    }
}
