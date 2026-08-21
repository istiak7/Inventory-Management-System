namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderLineResponse(
        int Id,
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int OrderedQuantity,
        int? ReceivedQuantity,
        decimal UnitPrice,
        decimal TotalAmount,
        int WarrantyMonths,
        string Status
    );
}
