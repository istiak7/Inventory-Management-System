namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public sealed record SaleDetailResponse(
        int Id,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        string CustomerAddress,
        int BranchId,
        string BranchName,
        string InvoiceNumber,
        string? Remarks,
        DateTime SaleDate,
        string Status,
        string SaleType,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        int ItemsCount,
        IReadOnlyList<SaleLineResponse> Items,
        IReadOnlyList<SalePaymentResponse> Payments
    );
}
