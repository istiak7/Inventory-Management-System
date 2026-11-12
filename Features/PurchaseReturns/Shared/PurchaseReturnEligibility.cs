using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.PurchaseReturns.Shared
{
    public sealed record PurchaseLineEligibility(
        int SupplierPurchaseDetailsId, int Received, int Returned, int Remaining, int InStock,
        IReadOnlyList<string> AvailableSerials, string? BlockedReason)
    {
        public bool CanReturn => BlockedReason == null && Remaining > 0;
    }

    // What can still go back to the supplier from one bill, line by line. The create command
    // and the "returnable" query share this rule.
    public static class PurchaseReturnEligibility
    {
        // The purchase must be loaded with SupplierPurchaseDetails -> ProductVariant -> Product.
        public static async Task<Dictionary<int, PurchaseLineEligibility>> ForPurchaseAsync(
            AppDbContext db, SupplierPurchase purchase, CancellationToken cancellationToken)
        {
            var returned = await db.PurchaseReturnLines
                .IgnoreQueryFilters()
                .Where(l => l.PurchaseReturn.PurchaseId == purchase.Id && l.PurchaseReturn.Status == ReturnStatus.Completed)
                .GroupBy(l => l.SupplierPurchaseDetailsId)
                .Select(g => new { g.Key, Quantity = g.Sum(l => l.Quantity) })
                .ToDictionaryAsync(x => x.Key, x => x.Quantity, cancellationToken);

            var variantIds = purchase.SupplierPurchaseDetails.Select(d => d.ProductVariantId).Distinct().ToList();
            var stock = await db.Stocks
                .IgnoreQueryFilters()
                .Where(s => s.BranchId == purchase.BranchId && variantIds.Contains(s.ProductVariantId))
                .ToDictionaryAsync(s => s.ProductVariantId, s => s.CurrentStock, cancellationToken);

            // Units of this bill still on the shelf at its branch (serialized lines only).
            var lineIds = purchase.SupplierPurchaseDetails.Select(d => d.Id).ToList();
            var serials = await db.ProductSerials
                .IgnoreQueryFilters()
                .Where(s => lineIds.Contains(s.SupplierPurchaseDetailsId)
                            && s.BranchId == purchase.BranchId
                            && s.Status == SerialStatus.InStock)
                .OrderBy(s => s.SerialNumber)
                .Select(s => new { s.SupplierPurchaseDetailsId, s.SerialNumber })
                .ToListAsync(cancellationToken);

            var result = new Dictionary<int, PurchaseLineEligibility>();
            foreach (var line in purchase.SupplierPurchaseDetails)
            {
                var received = line.ReceivedQuantity ?? 0;
                var back = returned.GetValueOrDefault(line.Id);
                var remaining = Math.Max(0, received - back);
                var onHand = stock.GetValueOrDefault(line.ProductVariantId);
                var lineSerials = serials.Where(s => s.SupplierPurchaseDetailsId == line.Id).Select(s => s.SerialNumber).ToList();
                string? blocked = null;

                if (purchase.Status != PurchaseStatus.Approved)
                    blocked = "Only an approved (fully received) purchase can be returned.";
                else if (line.Status == LineStatus.Rejected || received == 0)
                    blocked = "Nothing was received on this line.";
                else if (!line.ProductVariant.Product.IsReturnable)
                    blocked = "This product is not returnable.";
                else if (remaining == 0)
                    blocked = "Already fully returned.";
                else if (line.ProductVariant.IsSerialized && lineSerials.Count == 0)
                    blocked = "None of this line's serial numbers are in stock here any more.";
                else if (onHand <= 0)
                    blocked = "Out of stock at this branch (sold or moved).";

                // A return can only send back what is still on the shelf.
                if (blocked == null)
                    remaining = Math.Min(remaining, line.ProductVariant.IsSerialized ? lineSerials.Count : onHand);

                result[line.Id] = new PurchaseLineEligibility(line.Id, received, back, remaining, onHand, lineSerials, blocked);
            }
            return result;
        }

        public static async Task<decimal> ReturnedGrossAsync(AppDbContext db, int purchaseId, CancellationToken cancellationToken) =>
            await db.PurchaseReturnLines
                .IgnoreQueryFilters()
                .Where(l => l.PurchaseReturn.PurchaseId == purchaseId && l.PurchaseReturn.Status == ReturnStatus.Completed)
                .SumAsync(l => (decimal?)(l.UnitPrice * l.Quantity), cancellationToken) ?? 0m;
    }
}
