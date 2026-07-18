using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Brands.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Brands.Shared.Repository
{
    public class BrandRepository(AppDbContext context)
        : BaseRepository<Brand>(context), IBrandRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<PagedResult<BrandResponse>> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.Id)
                .Select(b => new BrandResponse(
                    b.Id,
                    b.Name,
                    b.Description,
                    b.LogoUrl,
                    b.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }
    }
}
