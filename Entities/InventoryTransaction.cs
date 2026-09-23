using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class InventoryTransaction : BaseEntity
    {
        public int BranchId { get; set; } 
        public int ProductVariantId { get; set; } 
        public int? SupplierPurchaseDetailsId { get; set; } 
        public InventoryTxnType TransactionType { get; set; } = InventoryTxnType.PurchaseIn;
        public int QuantityIn { get; set; }
        public int QuantityOut { get; set; }
        public int BalanceAfter { get; set; }   
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public required Branch Branch { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public SupplierPurchaseDetails? SupplierPurchaseDetails { get; set; }
    }
}
