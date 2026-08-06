using Inventory_Management_System.Database;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Shared
{
    /// <summary>
    /// The one place that answers "how long is this unit covered for, and by which sale?".
    /// The eligibility screen and the claim-intake handler must agree to the day, so neither
    /// computes it itself — a counter that is shown a green card has to be able to open the claim.
    /// </summary>
    public static class WarrantyTerms
    {
        /// <summary>
        /// The day cover ends: the sale date plus the months sold with the line. Null when the unit
        /// was never sold or carries no warranty at all.
        /// </summary>
        public static DateTime? ExpiryFor(DateTime? soldDate, int? warrantyMonths) =>
            soldDate.HasValue && warrantyMonths is > 0
                ? soldDate.Value.AddMonths(warrantyMonths.Value)
                : null;

        /// <summary>
        /// Cover is compared by DATE, not by instant: a unit sold at 6pm on the 3rd is still
        /// covered at 9am on its expiry day, which is how a customer reads "12 months".
        /// </summary>
        public static bool IsUnderWarranty(DateTime? expiry, DateTime asOf) =>
            expiry.HasValue && expiry.Value.Date >= asOf.Date;

        /// <summary>Whole days of cover left; 0 once expired (never negative).</summary>
        public static int DaysRemaining(DateTime? expiry, DateTime asOf) =>
            expiry.HasValue
                ? Math.Max(0, (int)(expiry.Value.Date - asOf.Date).TotalDays)
                : 0;

        /// <summary>
        /// The sale line whose warranty this unit runs on. Normally the unit's own line — but a
        /// unit issued as a warranty replacement never had a sale of its own, so it inherits the
        /// line of the claim that put it in the customer's hands (see <see cref="Entities.WarrantyClaim"/>:
        /// the swap is recorded on the claim, the original sale is left untouched).
        /// </summary>
        public static async Task<int?> ResolveSaleDetailsIdAsync(
            AppDbContext dbContext, int productSerialId, CancellationToken cancellationToken)
        {
            var ownLineId = await dbContext.SaleDetails
                .AsNoTracking()
                .Where(d => d.ProductSerialId == productSerialId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (ownLineId.HasValue)
                return ownLineId;

            // Latest wins: a unit can itself be replaced later, and only the most recent swap
            // describes the cover the customer is holding today.
            return await dbContext.WarrantyClaims
                .AsNoTracking()
                .Where(c => c.ReplacementSerialId == productSerialId)
                .OrderByDescending(c => c.Id)
                .Select(c => (int?)c.SaleDetailsId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
