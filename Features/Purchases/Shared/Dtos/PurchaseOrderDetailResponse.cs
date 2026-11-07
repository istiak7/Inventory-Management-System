namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderDetailLineResponse(
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
        // Serials received into stock against this line.
        IReadOnlyList<string> SerialNumbers,
        // Serials entered on the order that have not been received yet.
        IReadOnlyList<string> PendingSerialNumbers
    );

    public sealed record PurchaseOrderDetailResponse(
        int Id,
        int SupplierId,
        string SupplierName,
        int BranchId,
        string BranchName,
        string? InvoiceNumber,
        string? Remarks,
        DateTime PurchaseDate,
        string Status,
        string? PurchaseType,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        decimal DiscountAmount,
        IReadOnlyList<PurchaseOrderDetailLineResponse> Items
    );
}
