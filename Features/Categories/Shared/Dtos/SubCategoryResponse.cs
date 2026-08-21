namespace Inventory_Management_System.Features.Categories.Shared.Dtos
{
    public sealed record SubCategoryResponse(
        int Id,
        string SubCategoryName,
        string Description,
        string ImageUrl,
        string Code,
        int ProductCategoryId,
        DateTime CreatedAt
    );
}
