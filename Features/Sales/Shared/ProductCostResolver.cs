using Inventory_Management_System.Database;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Shared
{
    /// <summary>
    /// Works out what a unit cost us, so a sale can record its cost of goods sold.
    ///
    /// The same variant can be bought in at different prices on different purchase orders, so
    /// there is no single "product cost" to read. Two cases:
    ///
    ///   Serialized units  - the serial is tied to the exact purchase line it arrived on, so we
    ///                       use that lot's real unit price.
    ///   Everything else   - weighted average over the quantity actually received on approved
    ///                       purchase lines, which is the conventional answer when individual
    ///                       units cannot be told apart.
    ///
    /// The caller snapshots the result onto SaleDetails.UnitCost, so later purchases at a
    /// different price never rewrite the profit on a sale that has already happened.
    /// </summary>
    public class ProductCostResolver(AppDbContext _dbContext)
    {
        private readonly Dictionary<int, decimal> _averageCostByVariant = [];

        /// <summary>Exact cost of the lot a serialized unit arrived on.</summary>
        public async Task<decimal> GetSerialCostAsync(int supplierPurchaseDetailsId, CancellationToken cancellationToken)
        {
            var cost = await _dbContext.SupplierPurchaseDetails
                .AsNoTracking()
                .Where(d => d.Id == supplierPurchaseDetailsId)
                .Select(d => (decimal?)d.UnitPrice)
                .FirstOrDefaultAsync(cancellationToken);

            return decimal.Round(cost ?? 0m, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Weighted average unit cost across everything received for this variant. Returns 0
        /// when nothing has been received yet, which leaves the sale recording no cost rather
        /// than inventing one.
        /// </summary>
        public async Task<decimal> GetAverageCostAsync(int productVariantId, CancellationToken cancellationToken)
        {
            if (_averageCostByVariant.TryGetValue(productVariantId, out var cached))
                return cached;

            // Only received quantity counts - an ordered-but-not-received line has not cost us
            // anything yet, and weighting by ordered quantity would skew the average.
            var lots = await _dbContext.SupplierPurchaseDetails
                .AsNoTracking()
                .Where(d => d.ProductVariantId == productVariantId
                            && d.ReceivedQuantity != null
                            && d.ReceivedQuantity > 0)
                .Select(d => new { Quantity = d.ReceivedQuantity!.Value, d.UnitPrice })
                .ToListAsync(cancellationToken);

            var totalQuantity = lots.Sum(l => (long)l.Quantity);
            var average = totalQuantity == 0
                ? 0m
                : decimal.Round(lots.Sum(l => l.UnitPrice * l.Quantity) / totalQuantity, 2, MidpointRounding.AwayFromZero);

            _averageCostByVariant[productVariantId] = average;
            return average;
        }
    }
}
