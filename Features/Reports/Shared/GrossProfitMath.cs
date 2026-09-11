namespace Inventory_Management_System.Features.Reports.Shared
{
    // Kept in one place so the daily and monthly reports round and divide identically.
    public static class GrossProfitMath
    {
        public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        /// <summary>Gross profit as a percentage of revenue; 0 when there was no revenue.</summary>
        public static decimal MarginPercent(decimal revenue, decimal grossProfit) =>
            revenue == 0 ? 0m : Round(grossProfit / revenue * 100m);
    }
}
