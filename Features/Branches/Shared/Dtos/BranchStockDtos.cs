namespace Inventory_Management_System.Features.Branches.Shared.Dtos
{
    public sealed record BranchStockItem(
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int CurrentStock);

    public sealed record BranchStockResponse(
        int BranchId,
        string BranchName,
        int TotalUnits,
        int VariantCount,
        IReadOnlyList<BranchStockItem> Items);

    public sealed record BranchVariantStockResponse(
        int BranchId,
        string BranchName,
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int CurrentStock);
}
