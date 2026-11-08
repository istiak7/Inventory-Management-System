using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.GetInvoiceSupplierLedger
{
    public sealed record GetInvoiceSupplierLedgerQuery(
        string InvoiceNumber,
        int SupplierId,
        // Set: a receipt for the payment made on that date against the invoice.
        // Null: the purchase invoice itself, issued when the purchase is approved, whether or
        // not anything has been paid (a 100% credit purchase still has an invoice).
        DateTime? Date
    ) : IRequest<Result>;
}
