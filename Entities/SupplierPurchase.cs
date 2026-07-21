namespace Inventory_Management_System.Entities
{
    public class SupplierPurchase : BaseEntity
    {
        public int SupplierId { get; set; } //FK
        public int BranchId { get; set; } //FK
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public string? InvoiceNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DueAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PurchaseType { get; set; } = "Cash"; // Cash or Credit

        // Navigation property
        public required Supplier Supplier { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SupplierPurchasePayment> SupplierPurchasePayments { get; set; } = [];
    }
}
