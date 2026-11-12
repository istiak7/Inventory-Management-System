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
        decimal OpeningBalance,
        // Cross-ledger view. Payable = what we owe this supplier (this ledger);
        // Receivable = what they owe us on the linked customer ledger (0 when not linked);
        // NetBalance = Receivable - Payable: positive = they owe us (green), negative = we owe them (red).
        decimal Receivable,
        decimal Payable,
        decimal NetBalance,
        int? CustomerId
    );
}
