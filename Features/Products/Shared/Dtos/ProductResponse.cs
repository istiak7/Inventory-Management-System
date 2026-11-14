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
        bool IsSerialized,
        // False = cannot be taken back (sales return) or sent back (purchase return).
        bool IsReturnable = true,
        // Optional warranty terms; null when none recorded.
        string? Warranty = null
    );
}
