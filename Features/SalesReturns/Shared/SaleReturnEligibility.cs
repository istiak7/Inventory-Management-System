using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.SalesReturns.Shared
{
    public sealed record SaleLineEligibility(int SaleDetailsId, int Sold, int Returned, int Remaining, string? BlockedReason)
    {
        public bool CanReturn => BlockedReason == null && Remaining > 0;
    }

    // What can still come back from one sale, line by line. The create command and the
    // "returnable" query use this same rule, so the screen never offers what the save refuses.
    public static class SaleReturnEligibility
    {
        // The sale must be loaded with SaleDetails -> ProductVariant -> Product and -> ProductSerial.
        public static async Task<Dictionary<int, SaleLineEligibility>> ForSaleAsync(
            AppDbContext db, CustomerSale sale, CancellationToken cancellationToken)
        {
            var returned = await db.SalesReturnLines
                .IgnoreQueryFilters()
                .Where(l => l.SalesReturn.SaleId == sale.Id && l.SalesReturn.Status == ReturnStatus.Completed)
                .GroupBy(l => l.SaleDetailsId)
                .Select(g => new { g.Key, Quantity = g.Sum(l => l.Quantity) })
                .ToDictionaryAsync(x => x.Key, x => x.Quantity, cancellationToken);

            var openClaims = await db.WarrantyClaims
                .IgnoreQueryFilters()
                .Where(c => c.SaleDetails.SaleId == sale.Id
                            && (c.Status == WarrantyClaimStatus.Open || c.Status == WarrantyClaimStatus.InRepair))
                .Select(c => new { c.SaleDetailsId, c.ClaimNumber })
                .ToListAsync(cancellationToken);

            var result = new Dictionary<int, SaleLineEligibility>();
            foreach (var line in sale.SaleDetails)
            {
                var back = returned.GetValueOrDefault(line.Id);
                var remaining = Math.Max(0, line.Quantity - back);
                string? blocked = null;

                if (sale.Status == SaleStatus.Voided || line.Status != SaleLineStatus.Completed)
                    blocked = "The sale is voided.";
                else if (!line.ProductVariant.Product.IsReturnable)
                    blocked = "This product is not returnable.";
                else if (remaining == 0)
                    blocked = "Already fully returned.";
                else if (openClaims.FirstOrDefault(c => c.SaleDetailsId == line.Id) is { } claim)
                    blocked = $"Warranty claim {claim.ClaimNumber} is open for this unit.";
                else if (line.ProductSerial is { } serial && serial.Status != SerialStatus.Sold)
                    blocked = $"Serial {serial.SerialNumber} is no longer with the customer ({serial.Status}).";

                result[line.Id] = new SaleLineEligibility(line.Id, line.Quantity, back, remaining, blocked);
            }
            return result;
        }

        // Gross value (unit price x qty) of what active returns already took back from this sale.
        public static async Task<decimal> ReturnedGrossAsync(AppDbContext db, int saleId, CancellationToken cancellationToken) =>
            await db.SalesReturnLines
                .IgnoreQueryFilters()
                .Where(l => l.SalesReturn.SaleId == saleId && l.SalesReturn.Status == ReturnStatus.Completed)
                .SumAsync(l => (decimal?)(l.UnitPrice * l.Quantity), cancellationToken) ?? 0m;
    }
}
