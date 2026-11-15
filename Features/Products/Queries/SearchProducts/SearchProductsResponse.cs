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
        int? AvailableStock,
        int SubCategoryId,
        string SubCategoryName,
        int BrandId,
        string BrandName,
        // Short name of the product's unit (pcs, kg ...); null when none is set.
        string? UomShortName = null
    );
}
