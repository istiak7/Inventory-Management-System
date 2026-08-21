using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Products.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Products.Shared.Repository
{
    public class ProductRepository(AppDbContext context)
        : BaseRepository<Product>(context), IProductRepository
    {
        private readonly AppDbContext _context = context;

        public Task<PagedResult<ProductResponse>> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default
        )
            => Project(_context.Products).ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

        public Task<PagedResult<ProductResponse>> GetBySubCategoryIdPagedAsync(
            int subCategoryId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default
        )
            => Project(_context.Products.Where(p => p.ProductSubCategoryId == subCategoryId))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

        public Task<PagedResult<ProductResponse>> GetByBrandIdPagedAsync(
            int brandId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default
        )
            => Project(_context.Products.Where(p => p.BrandId == brandId))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

        private static IQueryable<ProductResponse> Project(IQueryable<Product> source) =>
            source
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.ProductName,
                    p.ProductDescription,
                    p.ProductImageUrl,
                    p.ProductSubCategoryId,
                    p.BrandId,
                    p.CreatedAt,
                    p.ProductVariants.OrderBy(v => v.Id).Select(v => v.Id).FirstOrDefault(),
                    p.ProductVariants.OrderBy(v => v.Id).Select(v => v.SKU).FirstOrDefault() ?? string.Empty,
                    p.ProductVariants.OrderBy(v => v.Id).Select(v => v.SellingPrice).FirstOrDefault(),
                    p.ProductVariants.OrderBy(v => v.Id).Select(v => v.IsSerialized).FirstOrDefault()));
    }
}
