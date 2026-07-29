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
            CancellationToken cancellationToken = default)
        {
            
            var query = from supplier in _context.Suppliers
                        orderby supplier.Id
                        select new SupplierResponse(
                            supplier.Id,
                            supplier.Group,
                            supplier.Name,
                            supplier.Description,
                            supplier.PhoneNumber,
                            supplier.Email,
                            supplier.NID,
                            (supplier.SupplierPurchases
                                .Where(sp => sp.Status == PurchaseStatus.Approved || sp.Status == PurchaseStatus.PartiallyReceived)
                                .Sum(sp => (decimal?)sp.TotalAmount) ?? 0m)
                            - (supplier.SupplierPayments
                                .Where(spp =>  spp.IsActive == 1)
                                .Sum(spp => (decimal?)spp.Amount) ?? 0m),

                            supplier.CreatedAt);

            return await query
                .AsNoTracking()
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

        }
    }
}
