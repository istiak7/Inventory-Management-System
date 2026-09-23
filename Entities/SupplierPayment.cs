namespace Inventory_Management_System.Entities
{
    public class SupplierPayment : BaseEntity
    {
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string PaymentMethod { get; set; } = "Cash"; 
        public string? Remarks { get; set; }

        //Navigation property
        public required Supplier Supplier { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SupplierPurchasePayment> SupplierPurchasePayments { get; set; } = [];
    }
}
