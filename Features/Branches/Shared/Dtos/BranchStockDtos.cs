namespace Inventory_Management_System.Features.Branches.Shared.Dtos
{
    public sealed record BranchStockItem(
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int CurrentStock);

    // A branch's full stock picture: total units on hand + per-variant breakdown.
    public sealed record BranchStockResponse(
        int BranchId,
        string BranchName,
        int TotalUnits,
        int VariantCount,
        IReadOnlyList<BranchStockItem> Items);

    // The number of stock for a single variant at a single branch.
    public sealed record BranchVariantStockResponse(
        int BranchId,
        string BranchName,
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int CurrentStock);
}
