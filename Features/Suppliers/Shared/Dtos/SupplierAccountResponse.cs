namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public sealed record SupplierAccountResponse(
        int SupplierId,
        string SupplierName,
        string Group,
        decimal TotalPurchases,
        decimal TotalPayments,
        decimal Balance,
        DateTime? LastTransactionDate,
        // Contact details, carried so an account statement / receipt can be printed
        // without a second round-trip to the supplier list.
        string PhoneNumber,
        string Email,
        string Address,
        decimal OpeningBalance
    );
}
