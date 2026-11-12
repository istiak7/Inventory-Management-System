namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerAccountResponse(
        int CustomerId,
        string CustomerName,
        string Group,
        decimal TotalSales,
        decimal TotalPayments,
        decimal Balance,
        DateTime? LastTransactionDate,
        string PhoneNumber,
        string Email,
        string Address,
        decimal OpeningBalance,
        // Cross-ledger view. Receivable = what this party owes us (this customer ledger);
        // Payable = what we owe them on the linked supplier ledger (0 when not linked);
        // NetBalance = Receivable - Payable: positive = they owe us (green), negative = we owe them (red).
        // Balance above stays this ledger's own balance, which payment forms limit against.
        decimal Receivable,
        decimal Payable,
        decimal NetBalance,
        int? SupplierId,
        bool IsCreditParty
    );
}
