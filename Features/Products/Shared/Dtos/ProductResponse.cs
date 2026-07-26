namespace Inventory_Management_System.Features.Products.Shared.Dtos
{
    // Catalog fields plus a summary of the product's primary (first) variant, so list/detail
    // screens keep showing SKU / price without a separate call. Full multi-variant listing
    // will be a dedicated variant query later.
    public sealed record ProductResponse(
        int Id,
        string ProductName,
        string ProductDescription,
        string ProductImageUrl,
        int ProductSubCategoryId,
        int BrandId,
        DateTime CreatedAt,
        int VariantId,
        string SKU,
        decimal SellingPrice,
        bool IsSerialized);
}
