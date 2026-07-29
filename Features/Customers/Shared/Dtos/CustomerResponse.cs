namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    /// <summary>
    /// Sales-side counterpart of <see cref="Suppliers.Shared.Dtos.SupplierResponse"/>. Carries the
    /// extra <see cref="Address"/> the Customer entity has, plus <see cref="CurrentBalance"/> so a
    /// seller can see what is already owed before agreeing to another credit sale.
    /// </summary>
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
        DateTime CreatedAt);
}
