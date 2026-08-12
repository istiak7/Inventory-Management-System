namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record InvoiceLineItemResponse(
        int SN,
        string ProductDescription,
        int WarrantyMonths,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice
    );

    public sealed record GetInvoiceSupplierLedgerResponse(
        string InvoiceNumber,
        DateTime Date,          // the payment date this receipt covers
        string SupplierName,
        string SupplierAddress,
        string MobileNumber,
        string BillType,        // SupplierPayment.PaymentMethod: Cash | Bank | bKash | ...
        string BranchName,
        IReadOnlyList<InvoiceLineItemResponse> Items,
        decimal TotalAmount,    // the invoice's total
        decimal PaidAmount,     // amount paid on THIS payment, against THIS invoice
        decimal DueBeforeAmount, // invoice due immediately before this payment
        decimal DueAmount       // invoice due immediately after this payment
    );
}
