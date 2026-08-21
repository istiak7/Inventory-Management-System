namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public sealed record SupplierResponse(
        int Id,
        string Group,
        string Name,
        string Description,
        string PhoneNumber,
        string Email,
        string NID,
        decimal OpeningBalance,
        DateTime CreatedAt
    );
}
