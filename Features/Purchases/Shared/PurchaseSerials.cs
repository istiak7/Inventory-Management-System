using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Purchases.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Purchases.Shared
{
    /// <summary>
    /// Serial numbers entered on a purchase order before the goods arrive.
    ///
    /// They are saved as ProductSerial rows with status Ordered. That reserves each number
    /// (the SerialNumber unique index covers every status) without putting anything in stock:
    /// every stock, sale, transfer and warranty query only looks at InStock / Sold. Approval
    /// (receive-purchase-order) turns them into InStock. Reject, delete, edit and "close short"
    /// remove the ones that will never arrive.
    /// </summary>
    public static class PurchaseSerials
    {
        // Same limit as the SerialNumber column.
        public const int MaxLength = 100;

        /// <summary>Trimmed, non-empty serials from a request line.</summary>
        public static List<string> Clean(IEnumerable<string?>? serials) =>
            (serials ?? [])
                .Select(s => s?.Trim() ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToList();

        /// <summary>
        /// Checks the serials on every line of a create/update request. Returns an error message,
        /// or null when everything is fine. <paramref name="variants"/> maps variant id to the
        /// variant (only IsSerialized and the product name are used).
        /// <paramref name="purchaseId"/> is the order being edited, whose own Ordered serials
        /// are about to be replaced and so do not count as taken.
        /// </summary>
        public static async Task<string?> ValidateAsync(
            AppDbContext db,
            IReadOnlyList<PurchaseItemRequest> items,
            IReadOnlyDictionary<int, ProductVariant> variants,
            int? purchaseId,
            CancellationToken ct)
        {
            var all = new List<string>();

            foreach (var item in items)
            {
                if (!variants.TryGetValue(item.ProductVariantId, out var variant))
                    continue; // the handler reports the missing variant itself

                var name = variant.Product?.ProductName ?? $"variant {variant.Id}";
                var serials = Clean(item.SerialNumbers);

                // The product master decides tracking; the order cannot override it.
                if (!variant.IsSerialized)
                {
                    if (serials.Count > 0)
                        return $"'{name}' is not serial-tracked, so serial numbers cannot be entered for it.";
                    continue;
                }

                if (serials.Count != item.Quantity)
                    return $"'{name}' is serial-tracked: enter exactly {item.Quantity} serial number(s), one per unit ({serials.Count} entered).";

                var tooLong = serials.FirstOrDefault(s => s.Length > MaxLength);
                if (tooLong != null)
                    return $"Serial number '{tooLong[..20]}…' is longer than {MaxLength} characters.";

                all.AddRange(serials);
            }

            // Case-insensitive inside one order: "abc1" and "ABC1" on the same invoice is a typo.
            var dupInOrder = all
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (dupInOrder.Count > 0)
                return $"Duplicate serial numbers in this purchase order: {string.Join(", ", dupInOrder)}.";

            var existing = await FindExistingAsync(db, all, purchaseId, ct);
            if (existing.Count > 0)
                return $"These serial numbers already exist in inventory or on another purchase order: {string.Join(", ", existing)}.";

            return null;
        }

        /// <summary>
        /// Which of <paramref name="serials"/> already exist anywhere (any branch, any status),
        /// ignoring the Ordered serials of <paramref name="excludePurchaseId"/>.
        /// </summary>
        public static async Task<List<string>> FindExistingAsync(
            AppDbContext db,
            IReadOnlyCollection<string> serials,
            int? excludePurchaseId,
            CancellationToken ct)
        {
            if (serials.Count == 0) return [];

            var query = db.ProductSerials
                .IgnoreQueryFilters()
                .Where(s => serials.Contains(s.SerialNumber));

            if (excludePurchaseId is int id)
                query = query.Where(s => !(s.Status == SerialStatus.Ordered
                                           && s.SupplierPurchaseDetails.PurchaseId == id));

            return await query.Select(s => s.SerialNumber).Distinct().ToListAsync(ct);
        }

        /// <summary>Adds the Ordered serials for one new purchase line.</summary>
        public static async Task ReserveAsync(
            AppDbContext db,
            SupplierPurchase purchase,
            SupplierPurchaseDetails detail,
            IEnumerable<string> serials,
            CancellationToken ct)
        {
            foreach (var sn in serials)
            {
                await db.ProductSerials.AddAsync(new ProductSerial
                {
                    ProductVariantId = detail.ProductVariantId,
                    BranchId = purchase.BranchId,
                    SerialNumber = sn,
                    Status = SerialStatus.Ordered,
                    WarrantyDays = detail.WarrantyDays,
                    ProductVariant = detail.ProductVariant,
                    SupplierPurchaseDetails = detail,
                    Branch = purchase.Branch,
                }, ct);
            }
        }

        /// <summary>
        /// Deletes Ordered (not yet received) serials of the given purchase lines. They were never
        /// stock and nothing references them, so they are removed outright, which also frees
        /// the numbers for another order.
        /// </summary>
        public static async Task ReleaseAsync(
            AppDbContext db,
            IEnumerable<int> detailIds,
            CancellationToken ct)
        {
            var ids = detailIds.ToList();
            if (ids.Count == 0) return;

            var pending = await db.ProductSerials
                .IgnoreQueryFilters()
                .Where(s => ids.Contains(s.SupplierPurchaseDetailsId) && s.Status == SerialStatus.Ordered)
                .ToListAsync(ct);

            db.ProductSerials.RemoveRange(pending);
        }
    }
}
