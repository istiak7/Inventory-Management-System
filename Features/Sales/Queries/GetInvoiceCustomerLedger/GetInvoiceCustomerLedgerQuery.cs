using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetInvoiceCustomerLedger
{
    public sealed record GetInvoiceCustomerLedgerQuery(
        string InvoiceNumber,
        int CustomerId,
        DateTime Date
    ) : IRequest<Result>;
}
