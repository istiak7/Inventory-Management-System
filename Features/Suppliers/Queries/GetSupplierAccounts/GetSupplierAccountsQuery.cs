using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierAccounts
{
    public sealed record GetSupplierAccountsQuery(int? SupplierId = null) : IRequest<Result>;
}
