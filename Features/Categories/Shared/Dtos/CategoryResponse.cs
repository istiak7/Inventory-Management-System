namespace Inventory_Management_System.Features.Categories.Shared.Dtos
{
    public sealed record CategoryResponse(
        int Id,
        string CategoryName,
        string Description,
        string ImageUrl,
        string Code,
        DateTime CreatedAt);
}
