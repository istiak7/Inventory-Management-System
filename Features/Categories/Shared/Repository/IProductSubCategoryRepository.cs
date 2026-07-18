using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Categories.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;

namespace Inventory_Management_System.Features.Categories.Shared.Repository
{
    public interface IProductSubCategoryRepository : IBaseRepository<ProductSubCategories>
    {
        Task<PagedResult<SubCategoryResponse>> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
