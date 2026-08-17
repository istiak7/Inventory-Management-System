namespace Inventory_Management_System.Features.Customers.Shared.Dtos
{
    public sealed record CustomerLedgerEntryResponse(
        int Id,
        int CustomerId,
        string CustomerName,
        string TransactionType,   // "Sale" | "Payment"
        DateTime TransactionDate,
        string Reference,
        decimal Debit,
        decimal Credit,
        decimal BalanceAfter,
        // Invoice(s) this row relates to: the sale's own invoice, or — for a payment —
        // the invoice(s) it was applied against. Empty for a payment with no allocation.
        IReadOnlyList<string> Invoices,
        // The sale's Remarks for a Sale row, or the payment's Remarks for a Payment row.
        string? Remarks
    );
}
