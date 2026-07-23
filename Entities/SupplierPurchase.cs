namespace Inventory_Management_System.Entities
{
    public class SupplierPurchase : BaseEntity
    {
        public int SupplierId { get; set; } //FK
        public int BranchId { get; set; } //FK
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public string? InvoiceNumber { get; set; }
        public string Status { get; set; } = "Pending";
        public string PurchaseType { get; set; } = "Cash"; // Cash or Credit
        public decimal TotalAmount { get; set; }   // invoice total = sum of the detail lines
        public decimal PaidAmount { get; set; }     // settled so far
        public decimal DueAmount { get; set; }      // outstanding = TotalAmount - PaidAmount

        // Navigation property
        public required Supplier Supplier { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SupplierPurchasePayment> SupplierPurchasePayments { get; set; } = [];
    }
}
