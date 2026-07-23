using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierTransactions
{
    public sealed record GetSupplierTransactionsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? SupplierId = null
    ) : IRequest<Result>;
}
