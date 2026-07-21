namespace Inventory_Management_System.Entities
{
    public class InventoryTransaction : BaseEntity
    {
        public int BranchId { get; set; } //FK
        public int ProductId { get; set; } //FK
        public int? SupplierPurchaseId { get; set; } //FK
        public string TransactionType { get; set; } = "Purchase"; // Purchase, Sale, Adjustment, Transfer
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // Navigation property
        public required Branch Branch { get; set; }
        public required Product Product { get; set; }
        public SupplierPurchase? SupplierPurchase { get; set; }
    }
}
