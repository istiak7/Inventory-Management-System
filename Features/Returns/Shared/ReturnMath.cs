using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Returns.Shared
{
    // How much a return is worth, from the original document's own prices.
    //
    // Sales and purchase bills carry one discount (and, for sales, one tax) for the whole
    // document, not per line. So returned lines take their share by value:
    //     worth of lines = their gross (unit price x qty) / document gross x document total
    //
    // Several partial returns must add up to exactly the document total when everything has
    // come back, so the share is taken on the running total (what was already returned plus
    // this return) and the earlier part is subtracted — rounding never piles up.
    public static class ReturnMath
    {
        public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        public static decimal Share(decimal gross, decimal documentGross, decimal documentTotal) =>
            documentGross <= 0 ? 0m : Round(gross * documentTotal / documentGross);

        // Worth of this return. `returnedGross` = gross of lines already returned (active returns).
        // `remainingValue` = what is still left on the document (Total - Returned): never exceeded,
        // and taken exactly when everything is now back.
        public static decimal ReturnTotal(
            decimal thisGross, decimal returnedGross, decimal documentGross, decimal documentTotal,
            decimal remainingValue, bool everythingBack)
        {
            if (everythingBack)
                return Math.Max(0m, remainingValue);
            var total = Share(returnedGross + thisGross, documentGross, documentTotal)
                        - Share(returnedGross, documentGross, documentTotal);
            return Math.Clamp(total, 0m, Math.Max(0m, remainingValue));
        }

        // Splits `total` over lines by their gross; the last line takes the rounding remainder.
        public static decimal[] Split(decimal total, IReadOnlyList<decimal> grosses)
        {
            var sum = grosses.Sum();
            var result = new decimal[grosses.Count];
            decimal used = 0;
            for (var i = 0; i < grosses.Count; i++)
            {
                result[i] = i == grosses.Count - 1
                    ? total - used
                    : sum <= 0 ? 0m : Round(total * grosses[i] / sum);
                used += result[i];
            }
            return result;
        }

        // The stock row of a variant at a branch, created at 0 when there is none yet.
        public static async Task<Stock> GetOrCreateStockAsync(
            AppDbContext db, Dictionary<int, Stock> cache, Branch branch, ProductVariant variant,
            CancellationToken cancellationToken)
        {
            if (cache.TryGetValue(variant.Id, out var stock))
                return stock;

            stock = await db.Stocks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.BranchId == branch.Id && s.ProductVariantId == variant.Id, cancellationToken);
            if (stock == null)
            {
                stock = new Stock
                {
                    BranchId = branch.Id,
                    ProductVariantId = variant.Id,
                    CurrentStock = 0,
                    Branch = branch,
                    ProductVariant = variant,
                };
                await db.Stocks.AddAsync(stock, cancellationToken);
            }
            cache[variant.Id] = stock;
            return stock;
        }
    }
}
