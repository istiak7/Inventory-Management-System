using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierTransactions
{
    public class GetSupplierTransactionsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-supplier-transactions", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? supplierId = null,
                int? branchId = null,
                string? invoiceNumber = null,
                string? transactionType = null,
                DateTime? startDate = null,
                DateTime? endDate = null) =>
            {
                var result = await mediator.Send(new GetSupplierTransactionsQuery(
                    pageNumber, pageSize, supplierId, branchId, invoiceNumber, transactionType, startDate, endDate));
                return Results.Ok(result);
            }).WithTags("Supplier");
        }
    }
}
