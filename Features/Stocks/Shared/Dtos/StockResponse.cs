namespace Inventory_Management_System.Features.Stocks.Shared.Dtos
{
    public sealed record StockResponse(
        int Id,
        int BranchId,
        string BranchName,
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int CurrentStock);
}
