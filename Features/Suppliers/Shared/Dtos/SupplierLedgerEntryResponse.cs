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
        decimal BalanceAfter
    );
}
