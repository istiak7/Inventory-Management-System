namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderListResponse(
        int Id,
        int SupplierId,
        string SupplierName,
        int BranchId,
        string BranchName,
        string? InvoiceNumber,
        DateTime PurchaseDate,
        string Status,
        string PurchaseType,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        int ItemsCount,
        IReadOnlyList<PurchaseOrderLineResponse> Items
    );
}
