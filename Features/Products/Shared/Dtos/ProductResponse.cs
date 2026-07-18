namespace Inventory_Management_System.Features.Products.Shared.Dtos
{
    public sealed record ProductResponse(
        int Id,
        string ProductName,
        string ProductDescription,
        string ProductImageUrl,
        string ProductCode,
        decimal ProductPrice,
        int ProductSubCategoryId,
        int BrandId,
        DateTime CreatedAt);
}
