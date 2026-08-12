namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderDetailLineResponse(
        int Id,   // SupplierPurchaseDetails id (the lot)
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int OrderedQuantity,
        int? ReceivedQuantity,
        decimal UnitPrice,
        decimal TotalAmount,
        int WarrantyMonths,
        string Status,
        IReadOnlyList<string> SerialNumbers   // populated only for serialized lines
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
        string PurchaseType,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        IReadOnlyList<PurchaseOrderDetailLineResponse> Items
    );
}
