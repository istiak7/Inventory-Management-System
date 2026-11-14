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
        string? SerialNumber,
        // Units taken back by sales returns; the invoice edit keeps the quantity at or above it.
        int ReturnedQuantity = 0
    );
}
