using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;

namespace Inventory_Management_System.Features.Suppliers.Queries.GetAllSuppliers
{
    public sealed record GetAllSuppliersQuery(int PageNumber = 1, int PageSize = 20)
        : IRequest<Result>;
}
