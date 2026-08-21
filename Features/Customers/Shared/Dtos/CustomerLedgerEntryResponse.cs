namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerLedgerEntryResponse(
        int Id,
        int CustomerId,
        string CustomerName,
        string TransactionType,
        DateTime TransactionDate,
        string Reference,
        decimal Debit,
        decimal Credit,
        decimal BalanceAfter,
        IReadOnlyList<string> Invoices,
        string? Remarks
    );
}
