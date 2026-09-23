namespace Inventory_Management_System.Features.Dashboard.Shared.Dtos
{
    /// <summary>Everything the dashboard page shows, in one response.</summary>
    public sealed record DashboardSummaryResponse(
        decimal StockValue,
        int ActiveItems,
        int LowStockCount,
        int OutOfStockCount,
        int LowStockThreshold,
        IReadOnlyList<DashboardMovementDay> MovementTrend,
        IReadOnlyList<DashboardTopProduct> TopMovedProducts,
        IReadOnlyList<DashboardCategoryValue> StockValueByCategory,
        IReadOnlyList<DashboardMonthRevenue> RevenueByMonth,
        IReadOnlyList<DashboardStockAlert> StockAlerts
    );

    /// <summary>Units that came in and went out on one day.</summary>
    public sealed record DashboardMovementDay(DateOnly Date, int Inbound, int Outbound);

    public sealed record DashboardTopProduct(int ProductVariantId, string Sku, string ProductName, int Moves);

    /// <summary>On-hand stock value (at average purchase cost) for one category.</summary>
    public sealed record DashboardCategoryValue(string CategoryName, decimal Value);

    /// <summary>Revenue and cost of goods sold for one month of the current year.</summary>
    public sealed record DashboardMonthRevenue(int Month, decimal Revenue, decimal Cost);

    /// <summary>A stock row (product at a branch) that is out of stock or running low.</summary>
    public sealed record DashboardStockAlert(
        int ProductVariantId,
        string Sku,
        string ProductName,
        string BranchName,
        int CurrentStock,
        string Status
    );
}
