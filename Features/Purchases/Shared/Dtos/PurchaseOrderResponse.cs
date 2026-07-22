namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderResponse(
        int Id,
        int SupplierId,
        int BranchId,
        DateTime PurchaseDate,
        string? InvoiceNumber,
        string Status,
        string PurchaseType,
        decimal TotalAmount,
        decimal TotalDueAmount,
        PaymentResponse? Payment
    );
}
