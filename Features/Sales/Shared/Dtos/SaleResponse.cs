namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public sealed record SaleResponse(
        int Id,
        int CustomerId,
        int BranchId,
        DateTime SaleDate,
        string InvoiceNumber,
        string? Remarks,
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
