using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetInvoiceCustomerLedger
{
    public class GetInvoiceCustomerLedgerEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-invoice-customer-ledger", async (
                IMediator mediator,
                string invoiceNumber,
                int customerId,
                DateTime date) =>
            {
                var result = await mediator.Send(new GetInvoiceCustomerLedgerQuery(invoiceNumber, customerId, date));
                return Results.Ok(result);
            }).WithTags("Sales");
        }
    }
}
