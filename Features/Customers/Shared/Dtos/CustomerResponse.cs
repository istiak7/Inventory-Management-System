namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerResponse(
        int Id,
        string Group,
        string Name,
        string Description,
        string PhoneNumber,
        string Email,
        string Address,
        string NID,
        decimal OpeningBalance,
        decimal CurrentBalance,
        DateTime CreatedAt,
        // Also a supplier (credit customer / supplier party), linked by phone number.
        int? SupplierId = null,
        bool IsCreditParty = false
    );
}
