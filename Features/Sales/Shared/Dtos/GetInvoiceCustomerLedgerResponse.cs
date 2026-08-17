namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public sealed record InvoiceLineItemResponse(
        int SN,
        string ProductDescription,
        int WarrantyMonths,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice
    );

    /// <summary>
    /// Printable receipt for one payment made against a customer sale invoice (full or
    /// partial) — a record of that money transaction, not a blank order form. Mirrors
    /// GetInvoiceSupplierLedgerResponse on the purchasing side.
    /// </summary>
    public sealed record GetInvoiceCustomerLedgerResponse(
        string InvoiceNumber,
        DateTime Date,          // the payment date this receipt covers
        string CustomerName,
        string CustomerAddress,
        string MobileNumber,
        string BillType,        // CustomerPayment.PaymentMethod: Cash | Bank | bKash | ...
        string BranchName,
        IReadOnlyList<InvoiceLineItemResponse> Items,
        decimal TotalAmount,    // the invoice's total
        decimal PaidAmount,     // amount paid on THIS payment, against THIS invoice
        decimal DueBeforeAmount, // invoice due immediately before this payment
        decimal DueAmount       // invoice due immediately after this payment
    );
}
