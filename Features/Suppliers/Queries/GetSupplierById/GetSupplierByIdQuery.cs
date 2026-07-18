using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetSupplierById
{
    public sealed record GetSupplierByIdQuery(int Id) : IRequest<Result>;
}
