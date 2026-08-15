namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    /// <summary>
    /// GET /get-sale/{id}. Everything in <see cref="SaleListResponse"/> plus every payment allocated
    /// to this sale — enough to render the invoice on its own.
    /// </summary>
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
        IReadOnlyList<SalePaymentResponse> Payments);
}
