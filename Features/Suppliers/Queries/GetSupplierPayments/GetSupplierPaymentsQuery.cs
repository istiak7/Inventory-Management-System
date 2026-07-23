using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierPayments
{
    public sealed record GetSupplierPaymentsQuery(int SupplierId) : IRequest<Result>;
}
