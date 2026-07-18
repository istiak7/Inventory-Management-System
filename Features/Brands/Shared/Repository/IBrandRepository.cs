using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Brands.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Features.Brands.Shared.Repository
{
    public interface IBrandRepository : IBaseRepository<Brand>
    {
        Task<PagedResult<BrandResponse>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
