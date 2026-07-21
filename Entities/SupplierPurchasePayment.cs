namespace Inventory_Management_System.Entities
{
    public class SupplierPurchasePayment : BaseEntity   // Junction Table
    {
        public int SupplierPurchaseId { get; set; }
        public int SupplierPaymentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime AllocationDate { get; set; } = DateTime.Now;

        // Navigation properties
        public required SupplierPurchase SupplierPurchase { get; set; }
        public required SupplierPayment SupplierPayment { get; set; }
    }
}
