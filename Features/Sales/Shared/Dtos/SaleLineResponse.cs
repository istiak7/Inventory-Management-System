namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    /// <summary>
    /// A sale line as read back from the database. Richer than <see cref="SaleItemResponse"/> — that
    /// one is built in-memory by the create handler and cannot carry the persisted row's Id, SKU or
    /// per-unit discount.
    /// </summary>
    public sealed record SaleLineResponse(
        int Id,
        int ProductVariantId,
        string Sku,
        string ProductName,
        bool IsSerialized,
        int Quantity,
        decimal UnitPrice,
        decimal? DiscountPerItem,
        decimal TotalAmount,
        int? WarrantyMonths,
        string Status,
        string? SerialNumber);
}
