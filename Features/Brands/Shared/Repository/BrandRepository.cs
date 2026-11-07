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
            string? search = null,
            CancellationToken cancellationToken = default
        )
        {
            var query = _context.Brands.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"%{search.Trim()}%";
                query = query.Where(b => EF.Functions.ILike(b.Name, term));
            }

            return await query
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
