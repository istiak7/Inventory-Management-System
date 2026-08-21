using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Customers.Queries.GetCustomerPaymentHistory
{
    public sealed record GetCustomerPaymentHistoryQuery(
        int CustomerId,
        int PageNumber = 1,
        int PageSize = 10,
        string? InvoiceNumber = null,
        int? PaymentId = null,
        string? PaymentMethod = null
    ) : IRequest<Result>;
}
