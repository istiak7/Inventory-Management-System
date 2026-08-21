using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Features.Suppliers.Shared.Repository
{
    public interface ISupplierRepository : IBaseRepository<Supplier>
    {
        Task<PagedResult<SupplierResponse>> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default
        );
    }
}
