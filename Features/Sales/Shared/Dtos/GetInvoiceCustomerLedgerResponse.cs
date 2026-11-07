namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public sealed record InvoiceLineItemResponse(
        int SN,
        string ProductDescription,
        int WarrantyDays,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice
    );

    public sealed record GetInvoiceCustomerLedgerResponse(
        string InvoiceNumber,
        DateTime Date,
        string CustomerName,
        string CustomerAddress,
        string MobileNumber,
        string BillType,
        string BranchName,
        IReadOnlyList<InvoiceLineItemResponse> Items,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueBeforeAmount,
        decimal DueAmount,
        // The lines add up to SubTotal; TotalAmount is SubTotal + TaxAmount - DiscountAmount.
        // Without these the printed receipt's lines did not add up to its total.
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        // Discount waived on this invoice during this very payment (0 when none).
        decimal DiscountNow
    );
}
