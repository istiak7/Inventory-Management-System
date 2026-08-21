namespace Inventory_Management_System.Features.Suppliers.Shared.Dtos
{
    public sealed record SupplierLedgerEntryResponse(
        int Id,
        int SupplierId,
        string SupplierName,
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
