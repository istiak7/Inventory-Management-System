namespace Inventory_Management_System.Entities.Common
{
    /// <summary>
    /// Implemented by append-only money ledgers whose rows carry a running balance
    /// (CustomerTransaction, SupplierTransaction). Lets ledger queries like "latest balance"
    /// be written once instead of per entity.
    /// </summary>
    public interface ILedgerEntry
    {
        int Id { get; }
        decimal BalanceAfter { get; }
    }
}
