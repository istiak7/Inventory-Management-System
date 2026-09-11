namespace Inventory_Management_System.Features.Stocks.Shared
{
    // Availability as the UI shows it. Always derived from the quantity on hand - nothing
    // stores this, so it cannot go stale.
    public static class StockStatuses
    {
        public const string InStock = "InStock";
        public const string OutOfStock = "OutOfStock";

        public static string For(int currentStock) => currentStock > 0 ? InStock : OutOfStock;
    }
}
