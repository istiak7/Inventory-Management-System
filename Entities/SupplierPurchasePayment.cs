namespace Inventory_Management_System.Entities
{
    public class SupplierPurchasePayment : BaseEntity 
    {
        public int SupplierPurchaseId { get; set; }
        public int SupplierPaymentId { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Discount waived on this invoice as part of this payment (0 when none).</summary>
        public decimal Discount { get; set; }
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public required SupplierPurchase SupplierPurchase { get; set; }
        public required SupplierPayment SupplierPayment { get; set; }
    }
}
