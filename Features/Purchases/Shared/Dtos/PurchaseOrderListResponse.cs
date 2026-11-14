namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderListResponse(
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
        int ItemsCount,
        IReadOnlyList<PurchaseOrderLineResponse> Items,
        // Value of goods already sent back to the supplier (the total itself is never reduced).
        decimal ReturnedAmount = 0
    );
}
