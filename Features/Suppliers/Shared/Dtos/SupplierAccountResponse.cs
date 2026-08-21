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
        string PhoneNumber,
        string Email,
        string Address,
        decimal OpeningBalance
    );
}
