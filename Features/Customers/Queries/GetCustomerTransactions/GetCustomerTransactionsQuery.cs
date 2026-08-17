using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerTransactions
{
    public sealed record GetCustomerTransactionsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? CustomerId = null,
        string? InvoiceNumber = null   // contains-search on the related invoice(s)
    ) : IRequest<Result>;
}
