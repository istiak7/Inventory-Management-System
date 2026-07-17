namespace Inventory_Management_System.Entities
{
    public class SupplierPayment : BaseEntity
    {
        public int SupplierId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string PaymentMethod { get; set; } = "Cash";

        //Navigation property
        public required Supplier Supplier { get; set; }
        public ICollection<SupplierPurchasePayment> supplierPurchasePayments { get; set; } = [];
    }
}
