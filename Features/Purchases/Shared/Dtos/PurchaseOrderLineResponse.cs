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
        int WarrantyDays,
        string Status,
        // Serials entered on the order that have not been received yet.
        IReadOnlyList<string> PendingSerialNumbers
    );
}
