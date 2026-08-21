namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
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
        string? SerialNumber
    );
}
