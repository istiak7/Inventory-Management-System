namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    /// <summary>Row of GET /get-all-sales. Sales-side mirror of PurchaseOrderListResponse.</summary>
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
        IReadOnlyList<SaleLineResponse> Items);
}
