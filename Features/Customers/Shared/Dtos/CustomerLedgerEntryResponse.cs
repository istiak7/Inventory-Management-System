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
        string? Remarks,
        // The sale behind a Sale row, so its invoice can be opened (null on other rows). A sale is
        // its own invoice, paid or not — a 100% credit sale prints one too.
        int? SaleId
    );
}
