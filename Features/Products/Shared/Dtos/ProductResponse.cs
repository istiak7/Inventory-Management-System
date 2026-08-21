namespace Inventory_Management_System.Features.Products.Shared.Dtos
{
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
        bool IsSerialized
    );
}
