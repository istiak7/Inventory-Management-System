using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerTransactions
{
    public sealed record GetCustomerTransactionsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? CustomerId = null,
        int? BranchId = null,
        string? InvoiceNumber = null,
        string? TransactionType = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null
    ) : IRequest<Result>;
}
