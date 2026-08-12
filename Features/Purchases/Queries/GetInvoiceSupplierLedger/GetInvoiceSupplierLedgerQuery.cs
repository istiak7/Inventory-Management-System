using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.GetInvoiceSupplierLedger
{
    public sealed record GetInvoiceSupplierLedgerQuery(
        string InvoiceNumber,
        int SupplierId,
        DateTime Date
    ) : IRequest<Result>;
}
