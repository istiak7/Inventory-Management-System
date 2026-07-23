using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierPaymentHistory
{
    public class GetSupplierPaymentHistoryEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-supplier-payment-history", async (
                IMediator mediator,
                int supplierId,
                int pageNumber = 1,
                int pageSize = 10,
                string? invoiceNumber = null,
                int? paymentId = null,
                string? paymentMethod = null) =>
            {
                var result = await mediator.Send(
                    new GetSupplierPaymentHistoryQuery(supplierId, pageNumber, pageSize, invoiceNumber, paymentId, paymentMethod));
                return Results.Ok(result);
            }).WithTags("Supplier");
        }
    }
}
