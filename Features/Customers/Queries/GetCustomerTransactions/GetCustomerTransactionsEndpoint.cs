using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerTransactions
{
    public class GetCustomerTransactionsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/get-customer-transactions", async (
                IMediator mediator,
                int pageNumber = 1,
                int pageSize = 20,
                int? customerId = null,
                int? branchId = null,
                string? search = null,
                string? transactionType = null,
                DateTime? startDate = null,
                DateTime? endDate = null) =>
            {
                var result = await mediator.Send(new GetCustomerTransactionsQuery(
                    pageNumber, pageSize, customerId, branchId, search, transactionType, startDate, endDate));
                return Results.Ok(result);
            }).WithTags("Customer");
        }
    }
}
