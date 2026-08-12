namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public sealed record SupplierLedgerEntryResponse(
        int Id,
        int SupplierId,
        string SupplierName,
        string TransactionType,   // "Purchase" | "Payment"
        DateTime TransactionDate,
        string Reference,
        decimal Debit,
        decimal Credit,
        decimal BalanceAfter,
        // Invoice(s) this row relates to: the purchase's own invoice, or — for a payment —
        // the invoice(s) it was applied against. Empty for a payment with no allocation.
        IReadOnlyList<string> Invoices,
        // The purchase order's Remarks for a Purchase row, or the payment's Remarks for a Payment row.
        string? Remarks
    );
}
