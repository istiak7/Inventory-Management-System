namespace Inventory_Management_System.Entities.Common
{
    public interface ILedgerEntry
    {
        int Id { get; }
        decimal BalanceAfter { get; }
    }
}
