using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
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
            string? search = null,
            CancellationToken cancellationToken = default
        )
        {
            var suppliers = _context.Suppliers.AsQueryable();

            // Search by name, phone or email.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = $"%{search.Trim()}%";
                suppliers = suppliers.Where(s =>
                    EF.Functions.ILike(s.Name, term) ||
                    EF.Functions.ILike(s.PhoneNumber, term) ||
                    EF.Functions.ILike(s.Email, term));
            }

            var query = from supplier in suppliers
                        orderby supplier.Id
                        select new SupplierResponse(
                            supplier.Id,
                            supplier.Group,
                            supplier.Name,
                            supplier.Description,
                            supplier.PhoneNumber,
                            supplier.Email,
                            supplier.NID,
                            supplier.OpeningBalance,
                            // The balance is the supplier ledger's latest running balance, the same
                            // number the supplier accounts page and the payment check use.
                            supplier.SupplierTransactions
                                .OrderByDescending(t => t.Id)
                                .Select(t => (decimal?)t.BalanceAfter)
                                .FirstOrDefault() ?? 0m,

                            supplier.CreatedAt);

            return await query
                .AsNoTracking()
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        }
    }
}
