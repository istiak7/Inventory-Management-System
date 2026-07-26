using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // Append-only stock ledger (mirrors SupplierTransaction on the money side). Every stock
    // movement is one immutable row; BalanceAfter is the running on-hand for (Branch, ProductVariant)
    // computed once at insert time. Stock.CurrentStock is the derived snapshot, never edited directly.
    public class InventoryTransaction : BaseEntity
    {
        public int BranchId { get; set; } //FK
        public int ProductVariantId { get; set; } //FK
        public int? SupplierPurchaseDetailsId { get; set; } //FK -> originating lot, where relevant
        public InventoryTxnType TransactionType { get; set; } = InventoryTxnType.PurchaseIn;
        public int QuantityIn { get; set; }
        public int QuantityOut { get; set; }
        public int BalanceAfter { get; set; }   // running on-hand for (Branch, ProductVariant) at insert time
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // Navigation property
        public required Branch Branch { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public SupplierPurchaseDetails? SupplierPurchaseDetails { get; set; }
    }
}
