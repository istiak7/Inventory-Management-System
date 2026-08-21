using Inventory_Management_System.Database;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Shared
{
    public static class WarrantyTerms
    {
        public static DateTime? ExpiryFor(DateTime? soldDate, int? warrantyMonths) =>
            soldDate.HasValue && warrantyMonths is > 0
                ? soldDate.Value.AddMonths(warrantyMonths.Value)
                : null;

        public static bool IsUnderWarranty(DateTime? expiry, DateTime asOf) =>
            expiry.HasValue && expiry.Value.Date >= asOf.Date;

        public static int DaysRemaining(DateTime? expiry, DateTime asOf) =>
            expiry.HasValue
                ? Math.Max(0, (int)(expiry.Value.Date - asOf.Date).TotalDays)
                : 0;

        public static async Task<int?> ResolveSaleDetailsIdAsync(
            AppDbContext dbContext,
            int productSerialId,
            CancellationToken cancellationToken
        )
        {
            var ownLineId = await dbContext.SaleDetails
                .AsNoTracking()
                .Where(d => d.ProductSerialId == productSerialId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (ownLineId.HasValue)
                return ownLineId;

            return await dbContext.WarrantyClaims
                .AsNoTracking()
                .Where(c => c.ReplacementSerialId == productSerialId)
                .OrderByDescending(c => c.Id)
                .Select(c => (int?)c.SaleDetailsId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
