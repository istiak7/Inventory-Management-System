namespace Inventory_Management_System.Features.Reports.Shared.Dtos
{
    /// <summary>One day's trading. Revenue is what the sale was worth after discounts; COGS is
    /// what those units cost us, taken from the cost snapshotted on each sale line.</summary>
    public sealed record DailyGrossProfitResponse(
        DateOnly Date,
        decimal Revenue,
        decimal CostOfGoodsSold,
        decimal GrossProfit,
        decimal GrossMarginPercent,
        int SalesCount,
        int UnitsSold
    );

    /// <summary>A month's trading, with the day-by-day breakdown it was summed from.</summary>
    public sealed record MonthlyGrossProfitResponse(
        int Year,
        int Month,
        decimal Revenue,
        decimal CostOfGoodsSold,
        decimal GrossProfit,
        decimal GrossMarginPercent,
        int SalesCount,
        int UnitsSold,
        IReadOnlyList<DailyGrossProfitResponse> Days
    );
}
