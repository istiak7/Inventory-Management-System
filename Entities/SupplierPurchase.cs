namespace Inventory_Management_System.Entities
{
    public class SupplierPurchase : BaseEntity
    {
        public int SupplierId { get; set; }
        public required string Invoice { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DueAmount { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

        // Navigation property
        public required Supplier Supplier { get; set; }
        public ICollection<SupplierPurchasePayment> SupplierPurchasePayments { get; set; } = [];

    }
}
