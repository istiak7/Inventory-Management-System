namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record InvoiceLineItemResponse(
        int SN,
        string ProductDescription,
        int WarrantyDays,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice
    );

    public sealed record GetInvoiceSupplierLedgerResponse(
        string InvoiceNumber,
        DateTime Date,
        string SupplierName,
        string SupplierAddress,
        string MobileNumber,
        string BillType,
        string BranchName,
        IReadOnlyList<InvoiceLineItemResponse> Items,
        decimal DiscountAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueBeforeAmount,
        decimal DueAmount,
        // Discount waived on this invoice during this very payment (0 when none).
        decimal DiscountNow
    );
}
