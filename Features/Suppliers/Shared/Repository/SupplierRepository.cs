using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Suppliers.Shared.Dtos;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using Inventory_Management_System.Shared.Repository;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Suppliers.Shared.Repository
{
    public class SupplierRepository(AppDbContext context)
        : BaseRepository<Supplier>(context), ISupplierRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<PagedResult<SupplierResponse>> GetAllPagedAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .Select(s => new SupplierResponse(
                    s.Id,
                    s.Group,
                    s.Name,
                    s.Description,
                    s.PhoneNumber,
                    s.Email,
                    s.NID,
                    s.OpeningBalance,
                    s.CreatedAt))
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }
    }
}
