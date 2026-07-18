namespace Inventory_Management_System.Features.Brands.Shared.Dtos
{
    public sealed record BrandResponse(
        int Id,
        string Name,
        string Description,
        string LogoUrl,
        DateTime CreatedAt);
}
