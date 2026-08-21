namespace Inventory_Management_System.Features.Products.Queries.SearchProducts
{
    public sealed record SearchProductsResponse(
        int VariantId,
        int ProductId,
        string ProductName,
        string SKU,
        string Barcode,
        decimal SellingPrice,
        bool IsSerialized,
        string AttributesJson,
        float Rank,
        int? AvailableStock
    );
}
