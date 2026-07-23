using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierPaymentHistory
{
    public sealed record GetSupplierPaymentHistoryQuery(
        int SupplierId,
        int PageNumber = 1,
        int PageSize = 10,
        string? InvoiceNumber = null,   // contains-search on the invoice number
        int? PaymentId = null,          // filter to a single payment; null = all payments
        string? PaymentMethod = null    // filter by payment method; null = all methods
    ) : IRequest<Result>;
}
