namespace Inventory_Management_System.Features.Quotations.Shared.Dtos
{
    public sealed record QuotationLineResponse(
        int Id,
        int ProductVariantId,
        string ProductName,
        string Sku,
        string Uom,
        string? Warranty,
        bool IsSerialized,
        int Quantity,
        decimal UnitPrice,
        decimal DiscountAmount,
        decimal TotalAmount
    );

    public sealed record QuotationListResponse(
        int Id,
        string QuotationNumber,
        DateTime QuotationDate,
        DateTime ValidUntil,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        // The effective status: an open quotation past its validity reads "Expired".
        string Status,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        int ItemsCount,
        int? ConvertedSaleId,
        string? ConvertedInvoiceNumber,
        string? CreatedBy
    );

    public sealed record QuotationDetailResponse(
        int Id,
        string QuotationNumber,
        DateTime QuotationDate,
        DateTime ValidUntil,
        int CustomerId,
        string CustomerName,
        string CustomerPhoneNumber,
        string CustomerAddress,
        string CustomerEmail,
        string Status,
        decimal SubTotal,
        decimal DiscountAmount,
        decimal TaxAmount,
        decimal TotalAmount,
        string? Notes,
        string? TermsAndConditions,
        int ItemsCount,
        IReadOnlyList<QuotationLineResponse> Items,
        int? ConvertedSaleId,
        string? ConvertedInvoiceNumber,
        DateTime? ConvertedAt,
        string? CreatedBy,
        DateTime CreatedAt
    );
}
