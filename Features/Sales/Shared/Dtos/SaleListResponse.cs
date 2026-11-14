namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public sealed record SaleListResponse(
        int Id,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
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
        DateTime? VoidedAt = null,
        string? VoidReason = null,
        // Value of goods already returned (the total itself is never reduced by a return).
        decimal ReturnedAmount = 0
    );
}
