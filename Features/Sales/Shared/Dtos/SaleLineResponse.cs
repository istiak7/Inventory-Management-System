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
        decimal TotalAmount,
        int? WarrantyDays,
        string Status,
        string? SerialNumber
    );
}
