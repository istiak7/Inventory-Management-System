namespace Inventory_Management_System.Entities
{

    public class CustomerPayment : BaseEntity
    {
        public int CustomerId { get; set; } 
        public int BranchId { get; set; } 
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string PaymentMethod { get; set; } = "Cash"; 
        public string? Remarks { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SaleCustomerPayment> SaleCustomerPayments { get; set; } = [];
    }
}
