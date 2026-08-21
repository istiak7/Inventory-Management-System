using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierPaymentHistory
{
    public sealed record GetSupplierPaymentHistoryQuery(
        int SupplierId,
        int PageNumber = 1,
        int PageSize = 10,
        string? InvoiceNumber = null,
        int? PaymentId = null,
        string? PaymentMethod = null
    ) : IRequest<Result>;
}
