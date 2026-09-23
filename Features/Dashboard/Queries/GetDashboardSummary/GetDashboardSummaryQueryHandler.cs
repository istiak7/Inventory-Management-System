using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Dashboard.Shared.Dtos;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Dashboard.Queries.GetDashboardSummary
{
    // Builds every dashboard figure from real data. Stock, movements and sales are branch
    // filtered by AppDbContext, so staff automatically see only their own branch.
    public class GetDashboardSummaryQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetDashboardSummaryQueryHandler> _logger
    ) : IRequestHandler<GetDashboardSummaryQuery, Result>
    {
        private const int TopProductsCount = 5;
        private const int StockAlertsCount = 5;

        public async Task<Result> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            var days = Math.Clamp(request.Days, 1, 365);
            var threshold = Math.Max(request.LowStockThreshold, 0);

            try
            {
                #region Stock: value, counts, categories, alerts

                // Average purchase cost per variant, from what was really received
                // (the same rule the sale uses for its cost of goods sold).
                var averageCosts = await _dbContext.SupplierPurchaseDetails
                    .AsNoTracking()
                    .Where(d => d.ReceivedQuantity != null && d.ReceivedQuantity > 0)
                    .GroupBy(d => d.ProductVariantId)
                    .Select(g => new
                    {
                        VariantId = g.Key,
                        Quantity = g.Sum(d => (decimal)d.ReceivedQuantity!.Value),
                        Amount = g.Sum(d => d.UnitPrice * d.ReceivedQuantity!.Value),
                    })
                    .ToDictionaryAsync(
                        x => x.VariantId,
                        x => x.Quantity == 0 ? 0m : x.Amount / x.Quantity,
                        cancellationToken);

                // One row per product at a branch.
                var stockRows = await _dbContext.Stocks
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.ProductVariantId,
                        s.ProductVariant.SKU,
                        s.ProductVariant.Product.ProductName,
                        CategoryName = s.ProductVariant.Product.ProductSubCategories.ProductCategories.CategoryName,
                        BranchName = s.Branch.Name,
                        s.CurrentStock,
                    })
                    .ToListAsync(cancellationToken);

                decimal ValueOf(int variantId, int quantity) =>
                    quantity * averageCosts.GetValueOrDefault(variantId);

                var stockValue = GrossProfitMath.Round(stockRows.Sum(r => ValueOf(r.ProductVariantId, r.CurrentStock)));

                var activeItems = stockRows
                    .Where(r => r.CurrentStock > 0)
                    .Select(r => r.ProductVariantId)
                    .Distinct()
                    .Count();

                bool IsLow(int stock) => stock > 0 && stock <= threshold;

                var lowStockCount = stockRows.Count(r => IsLow(r.CurrentStock));
                var outOfStockCount = stockRows.Count(r => r.CurrentStock <= 0);

                var valueByCategory = stockRows
                    .Where(r => r.CurrentStock > 0)
                    .GroupBy(r => r.CategoryName)
                    .Select(g => new DashboardCategoryValue(
                        g.Key,
                        GrossProfitMath.Round(g.Sum(r => ValueOf(r.ProductVariantId, r.CurrentStock)))))
                    .Where(c => c.Value > 0)
                    .OrderByDescending(c => c.Value)
                    .ToList();

                // Out of stock first, then the lowest stock.
                var stockAlerts = stockRows
                    .Where(r => r.CurrentStock <= 0 || IsLow(r.CurrentStock))
                    .OrderBy(r => r.CurrentStock)
                    .ThenBy(r => r.ProductName)
                    .Take(StockAlertsCount)
                    .Select(r => new DashboardStockAlert(
                        r.ProductVariantId,
                        r.SKU,
                        r.ProductName,
                        r.BranchName,
                        r.CurrentStock,
                        r.CurrentStock <= 0 ? "out_of_stock" : "low_stock"))
                    .ToList();

                #endregion

                #region Movements: daily trend and top moved products

                // Days are the shop's calendar days (BusinessClock); the stored dates are UTC.
                var today = BusinessClock.Today;
                var firstDay = today.AddDays(-(days - 1));
                var from = BusinessClock.StartOfDayUtc(firstDay);
                var to = BusinessClock.StartOfDayUtc(today.AddDays(1));

                var movements = _dbContext.InventoryTransactions
                    .AsNoTracking()
                    .Where(t => t.TransactionDate >= from && t.TransactionDate < to);

                // Grouped here (not in SQL) so each movement lands on its local day.
                var movementRows = await movements
                    .Select(t => new { t.TransactionDate, t.QuantityIn, t.QuantityOut })
                    .ToListAsync(cancellationToken);

                var perDay = movementRows
                    .GroupBy(t => BusinessClock.LocalDateOf(t.TransactionDate))
                    .ToDictionary(
                        g => g.Key,
                        g => (Inbound: g.Sum(t => t.QuantityIn), Outbound: g.Sum(t => t.QuantityOut)));

                // Every day in the range, with 0 on days without movements, so the chart has no gaps.
                var movementTrend = Enumerable.Range(0, days)
                    .Select(i => DateOnly.FromDateTime(firstDay.AddDays(i)))
                    .Select(d => perDay.TryGetValue(d, out var m)
                        ? new DashboardMovementDay(d, m.Inbound, m.Outbound)
                        : new DashboardMovementDay(d, 0, 0))
                    .ToList();

                var topMoved = await movements
                    .GroupBy(t => new
                    {
                        t.ProductVariantId,
                        t.ProductVariant.SKU,
                        t.ProductVariant.Product.ProductName,
                    })
                    .Select(g => new
                    {
                        g.Key.ProductVariantId,
                        g.Key.SKU,
                        g.Key.ProductName,
                        Moves = g.Sum(t => t.QuantityIn + t.QuantityOut),
                    })
                    .OrderByDescending(x => x.Moves)
                    .Take(TopProductsCount)
                    .ToListAsync(cancellationToken);

                var topMovedProducts = topMoved
                    .Select(x => new DashboardTopProduct(x.ProductVariantId, x.SKU, x.ProductName, x.Moves))
                    .ToList();

                #endregion

                #region Revenue vs cost per month (this year)

                // Same revenue and cost rules as the gross profit reports.
                var yearStart = BusinessClock.StartOfDayUtc(new DateTime(today.Year, 1, 1));
                var yearEnd = BusinessClock.StartOfDayUtc(new DateTime(today.Year + 1, 1, 1));
                var byDay = await _dbContext.CustomerSales
                    .AsNoTracking()
                    .Where(s => s.SaleDate >= yearStart && s.SaleDate < yearEnd)
                    .WhereCountsTowardProfit()
                    .ToDailyGrossProfitAsync(cancellationToken);

                var revenueByMonth = Enumerable.Range(1, today.Month)
                    .Select(month =>
                    {
                        var monthDays = byDay.Where(d => d.Day.Month == month).ToList();
                        return new DashboardMonthRevenue(
                            month,
                            GrossProfitMath.Round(monthDays.Sum(d => d.Totals.Revenue)),
                            GrossProfitMath.Round(monthDays.Sum(d => d.Totals.CostOfGoodsSold)));
                    })
                    .ToList();

                #endregion

                var response = new DashboardSummaryResponse(
                    stockValue,
                    activeItems,
                    lowStockCount,
                    outOfStockCount,
                    threshold,
                    movementTrend,
                    topMovedProducts,
                    valueByCategory,
                    revenueByMonth,
                    stockAlerts);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Dashboard summary retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building the dashboard summary");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while loading the dashboard."
                };
            }
        }
    }
}
