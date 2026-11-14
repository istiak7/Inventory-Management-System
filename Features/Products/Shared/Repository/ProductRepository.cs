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
            string? search = null,
            int? subCategoryId = null,
            int? brandId = null,
            CancellationToken cancellationToken = default
        )
        {
            var source = _context.Products.AsQueryable();

            if (subCategoryId is int sc)
                source = source.Where(p => p.ProductSubCategoryId == sc);

            if (brandId is int b)
                source = source.Where(p => p.BrandId == b);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"%{search.Trim()}%";
                source = source.Where(p => EF.Functions.ILike(p.ProductName, term));
            }

            return Project(source).ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }

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
                    p.ProductVariants.OrderBy(v => v.Id).Select(v => v.IsSerialized).FirstOrDefault(),
                    p.IsReturnable,
                    p.Warranty));
    }
}
