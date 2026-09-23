namespace Inventory_Management_System.Entities
{
    public class SaleCustomerPayment : BaseEntity   
    {
        public int SaleId { get; set; } 
        public int CustomerPaymentId { get; set; } 
        public decimal Amount { get; set; }
        public DateTime AllocationDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public required CustomerSale CustomerSale { get; set; }
        public required CustomerPayment CustomerPayment { get; set; }
    }
}
