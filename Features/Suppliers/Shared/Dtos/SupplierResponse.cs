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
        // What was owed before using the system (typed in when the supplier was created).
        decimal OpeningBalance,
        // What is owed now: the supplier ledger's latest running balance.
        decimal Balance,
        DateTime CreatedAt
    );
}
