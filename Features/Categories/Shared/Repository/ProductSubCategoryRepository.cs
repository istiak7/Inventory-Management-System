using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Categories.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Categories.Shared.Repository
{
    public class ProductSubCategoryRepository(AppDbContext context)
        : BaseRepository<ProductSubCategories>(context), IProductSubCategoryRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<PagedResult<SubCategoryResponse>> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await _context.ProductSubCategories
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .Select(s => new SubCategoryResponse(
                    s.Id,
                    s.SubCategoryName,
                    s.Description,
                    s.ImageUrl,
                    s.Code,
                    s.ProductCategoryId,
                    s.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }

        public async Task<PagedResult<SubCategoryResponse>> GetByCategoryIdPagedAsync(
            int categoryId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await _context.ProductSubCategories
                .AsNoTracking()
                .Where(s => s.ProductCategoryId == categoryId)
                .OrderBy(s => s.Id)
                .Select(s => new SubCategoryResponse(
                    s.Id,
                    s.SubCategoryName,
                    s.Description,
                    s.ImageUrl,
                    s.Code,
                    s.ProductCategoryId,
                    s.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }
    }
}
