using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierTransactions
{
    public sealed record GetSupplierTransactionsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? SupplierId = null,
        string? InvoiceNumber = null   // contains-search on the related invoice(s)
    ) : IRequest<Result>;
}
