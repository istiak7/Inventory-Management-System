namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    public sealed record SaleItemResponse(
        int ProductVariantId,
        string ProductName,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        int? WarrantyMonths
    );
}
