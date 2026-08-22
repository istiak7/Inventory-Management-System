using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.GetInvoiceSupplierLedger
{
    public class GetInvoiceSupplierLedgerEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-invoice-supplier-ledger", async (
                IMediator mediator,
                string invoiceNumber,
                int supplierId,
                DateTime date) =>
            {
                var result = await mediator.Send(new GetInvoiceSupplierLedgerQuery(invoiceNumber, supplierId, date));
                return Results.Ok(result);
            }).WithTags("Purchase").RequirePermission(Permissions.PurchasesView);
        }
    }
}
