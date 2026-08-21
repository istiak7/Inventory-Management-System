namespace Inventory_Management_System.Features.Products.Shared.Dtos
{
    public sealed record ProductVariantResponse(
        int Id,
        int ProductId,
        string ProductName,
        string SKU,
        string Barcode,
        decimal SellingPrice,
        bool IsSerialized,
        string AttributesJson,
        DateTime CreatedAt
    );
}
