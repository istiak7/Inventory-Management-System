namespace Inventory_Management_System.Entities
{
    public class SaleCustomerPayment : BaseEntity   // Junction Table
    {
        public int SaleId { get; set; } //FK
        public int CustomerPaymentId { get; set; } //FK
        public decimal Amount { get; set; }
        public DateTime AllocationDate { get; set; } = DateTime.Now;

        // Navigation properties
        public required CustomerSale CustomerSale { get; set; }
        public required CustomerPayment CustomerPayment { get; set; }
    }
}
