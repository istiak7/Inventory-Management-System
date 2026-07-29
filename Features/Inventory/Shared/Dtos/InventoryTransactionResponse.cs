namespace Inventory_Management_System.Features.Inventory.Shared.Dtos
{
    public sealed record InventoryTransactionResponse(
        int Id,
        int BranchId,
        string BranchName,
        int ProductVariantId,
        string SKU,
        string ProductName,
        string TransactionType,
        int QuantityIn,
        int QuantityOut,
        int BalanceAfter,
        DateTime TransactionDate,
        int? SupplierPurchaseDetailsId,
        string? InvoiceNumber);
}
