using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Shared;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Reports.Shared
{
    public sealed record GrossProfitTotals(
        decimal Revenue,
        decimal CostOfGoodsSold,
        decimal GrossProfit,
        int SalesCount,
        int UnitsSold
    );

    /// <summary>
    /// The single definition of what counts toward gross profit and how it is added up, so the
    /// daily and monthly reports can never drift apart.
    /// </summary>
    public static class GrossProfitQueryExtensions
    {
        /// <summary>
        /// Only completed sales that still exist. Anything draft, cancelled or soft-deleted is
        /// excluded, as is any line that is not itself completed.
        /// </summary>
        public static IQueryable<CustomerSale> WhereCountsTowardProfit(this IQueryable<CustomerSale> sales) =>
            sales.Where(s => s.Status == SaleStatus.Completed
                             && s.IsActive != (int)EntityStatus.Deleted);

        public static async Task<GrossProfitTotals> ToGrossProfitTotalsAsync(
            this IQueryable<CustomerSale> sales,
            CancellationToken cancellationToken)
        {
            // Revenue is what we actually sold for: the line subtotal less any whole-invoice
            // discount. Tax is excluded - it is collected on behalf of the government, it was
            // never ours to profit from.
            //
            // Cost comes from the snapshot taken on each line at the time of sale, so restocking
            // at a new price later cannot retroactively change a past day's profit.
            var rows = await sales
                .Select(s => new
                {
                    Revenue = s.SubTotal - s.DiscountAmount,
                    CostOfGoodsSold = s.SaleDetails
                        .Where(d => d.Status == SaleLineStatus.Completed
                                    && d.IsActive != (int)EntityStatus.Deleted)
                        .Sum(d => (decimal?)(d.UnitCost * d.Quantity)) ?? 0m,
                    UnitsSold = s.SaleDetails
                        .Where(d => d.Status == SaleLineStatus.Completed
                                    && d.IsActive != (int)EntityStatus.Deleted)
                        .Sum(d => (int?)d.Quantity) ?? 0,
                })
                .ToListAsync(cancellationToken);

            var revenue = GrossProfitMath.Round(rows.Sum(r => r.Revenue));
            var cogs = GrossProfitMath.Round(rows.Sum(r => r.CostOfGoodsSold));

            return new GrossProfitTotals(
                revenue,
                cogs,
                GrossProfitMath.Round(revenue - cogs),
                rows.Count,
                rows.Sum(r => r.UnitsSold));
        }

        /// <summary>
        /// Same totals, but grouped by calendar day. Used by the monthly report so its daily
        /// breakdown and its overall figure come from one query.
        /// </summary>
        public static async Task<IReadOnlyList<(DateOnly Day, GrossProfitTotals Totals)>> ToDailyGrossProfitAsync(
            this IQueryable<CustomerSale> sales,
            CancellationToken cancellationToken)
        {
            var rows = await sales
                .Select(s => new
                {
                    s.SaleDate,
                    Revenue = s.SubTotal - s.DiscountAmount,
                    CostOfGoodsSold = s.SaleDetails
                        .Where(d => d.Status == SaleLineStatus.Completed
                                    && d.IsActive != (int)EntityStatus.Deleted)
                        .Sum(d => (decimal?)(d.UnitCost * d.Quantity)) ?? 0m,
                    UnitsSold = s.SaleDetails
                        .Where(d => d.Status == SaleLineStatus.Completed
                                    && d.IsActive != (int)EntityStatus.Deleted)
                        .Sum(d => (int?)d.Quantity) ?? 0,
                })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => BusinessClock.LocalDateOf(r.SaleDate))
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var revenue = GrossProfitMath.Round(g.Sum(r => r.Revenue));
                    var cogs = GrossProfitMath.Round(g.Sum(r => r.CostOfGoodsSold));
                    return (g.Key, new GrossProfitTotals(
                        revenue,
                        cogs,
                        GrossProfitMath.Round(revenue - cogs),
                        g.Count(),
                        g.Sum(r => r.UnitsSold)));
                })
                .ToList();
        }

        // ---------------------------------------------------------------- returns
        // A sales return takes its revenue (net of discount, without tax) off the day it was
        // returned, not the original sale day. Restocked units also take their cost back out;
        // damaged units do not, so their cost stays as a loss.

        public static IQueryable<SalesReturn> WhereCountsTowardProfit(this IQueryable<SalesReturn> returns) =>
            returns.Where(r => r.Status == ReturnStatus.Completed
                               && r.IsActive != (int)EntityStatus.Deleted);

        private static Task<List<ReturnProfitRow>> ReturnRowsAsync(
            IQueryable<SalesReturn> returns, CancellationToken cancellationToken) =>
            returns
                .Select(r => new ReturnProfitRow(
                    r.ReturnDate,
                    r.SubTotal - r.DiscountAmount,
                    r.Lines.Where(l => l.Restock).Sum(l => (decimal?)(l.UnitCost * l.Quantity)) ?? 0m,
                    r.Lines.Sum(l => (int?)l.Quantity) ?? 0))
                .ToListAsync(cancellationToken);

        private sealed record ReturnProfitRow(DateTime ReturnDate, decimal Revenue, decimal CostOfGoodsSold, int Units);

        private static GrossProfitTotals Less(GrossProfitTotals totals, IEnumerable<ReturnProfitRow> rows)
        {
            var list = rows.ToList();
            if (list.Count == 0) return totals;
            var revenue = GrossProfitMath.Round(totals.Revenue - list.Sum(r => r.Revenue));
            var cogs = GrossProfitMath.Round(totals.CostOfGoodsSold - list.Sum(r => r.CostOfGoodsSold));
            return new GrossProfitTotals(
                revenue, cogs, GrossProfitMath.Round(revenue - cogs),
                totals.SalesCount, totals.UnitsSold - list.Sum(r => r.Units));
        }

        // Totals less the returns made in the same period (the caller filters `returns` the same way).
        public static async Task<GrossProfitTotals> LessReturnsAsync(
            this GrossProfitTotals totals, IQueryable<SalesReturn> returns, CancellationToken cancellationToken) =>
            Less(totals, await ReturnRowsAsync(returns, cancellationToken));

        // Day-by-day totals less each day's returns; a day with only returns gets its own row.
        public static async Task<IReadOnlyList<(DateOnly Day, GrossProfitTotals Totals)>> LessReturnsAsync(
            this IReadOnlyList<(DateOnly Day, GrossProfitTotals Totals)> byDay, IQueryable<SalesReturn> returns,
            CancellationToken cancellationToken)
        {
            var returnsByDay = (await ReturnRowsAsync(returns, cancellationToken))
                .GroupBy(r => BusinessClock.LocalDateOf(r.ReturnDate))
                .ToDictionary(g => g.Key, g => g.ToList());
            if (returnsByDay.Count == 0) return byDay;

            var empty = new GrossProfitTotals(0m, 0m, 0m, 0, 0);
            return byDay.Select(d => d.Day)
                .Union(returnsByDay.Keys)
                .OrderBy(day => day)
                .Select(day =>
                {
                    var totals = byDay.FirstOrDefault(d => d.Day == day).Totals ?? empty;
                    return (day, Less(totals, returnsByDay.GetValueOrDefault(day) ?? []));
                })
                .ToList();
        }
    }
}
