using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierPayments
{
    public class GetSupplierPaymentsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-supplier-payments", async (IMediator mediator, int supplierId) =>
            {
                var result = await mediator.Send(new GetSupplierPaymentsQuery(supplierId));
                return Results.Ok(result);
            }).WithTags("Supplier");
        }
    }
}
