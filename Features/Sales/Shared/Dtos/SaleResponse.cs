namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    // The created sale == the invoice. Payment is null when nothing was paid at sale time.
    public sealed record SaleResponse(
        int Id,
        int CustomerId,
        int BranchId,
        DateTime SaleDate,
        string InvoiceNumber,
        string Status,
        string SaleType,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        List<SaleItemResponse> Items,
        SalePaymentResponse? Payment
    );
}
