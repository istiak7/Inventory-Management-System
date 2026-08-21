using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Categories.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Categories.Shared.Repository
{
    public class ProductCategoryRepository(AppDbContext context)
        : BaseRepository<ProductCategories>(context), IProductCategoryRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<PagedResult<CategoryResponse>> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.ProductCategories
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => new CategoryResponse(
                    c.Id,
                    c.CategoryName,
                    c.Description,
                    c.ImageUrl,
                    c.Code,
                    c.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }
    }
}
