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

        public async Task<PagedResult<ProductResponse>> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.ProductName,
                    p.ProductDescription,
                    p.ProductImageUrl,
                    p.ProductCode,
                    p.ProductPrice,
                    p.ProductSubCategoryId,
                    p.BrandId,
                    p.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }

        public async Task<PagedResult<ProductResponse>> GetBySubCategoryIdPagedAsync(
            int subCategoryId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.ProductSubCategoryId == subCategoryId)
                .OrderBy(p => p.Id)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.ProductName,
                    p.ProductDescription,
                    p.ProductImageUrl,
                    p.ProductCode,
                    p.ProductPrice,
                    p.ProductSubCategoryId,
                    p.BrandId,
                    p.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }

        public async Task<PagedResult<ProductResponse>> GetByBrandIdPagedAsync(
            int brandId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.BrandId == brandId)
                .OrderBy(p => p.Id)
                .Select(p => new ProductResponse(
                    p.Id,
                    p.ProductName,
                    p.ProductDescription,
                    p.ProductImageUrl,
                    p.ProductCode,
                    p.ProductPrice,
                    p.ProductSubCategoryId,
                    p.BrandId,
                    p.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }
    }
}
