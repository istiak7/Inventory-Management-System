namespace Inventory_Management_System.Entities
{
    public class SupplierTransaction : BaseEntity
    {
        public int SupplierId { get; set; }
        public string TransactionType { get; set; } = "Purchase";
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public decimal Amount { get; set; }
        public int SupplierPurchaseId { get; set; }
        public int SupplierPaymentId { get; set; }

        // Navigation property
        public required Supplier Supplier { get; set; }
        public SupplierPurchase? SupplierPurchase { get; set; }
        public SupplierPayment? SupplierPayment { get; set; }
    }
}
