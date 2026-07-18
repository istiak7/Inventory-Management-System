using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Products.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Features.Products.Shared.Repository
{
    public interface IProductRepository : IBaseRepository<Product>
    {
        Task<PagedResult<ProductResponse>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResult<ProductResponse>> GetBySubCategoryIdPagedAsync(int subCategoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResult<ProductResponse>> GetByBrandIdPagedAsync(int brandId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
