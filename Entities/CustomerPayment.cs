namespace Inventory_Management_System.Entities
{
    // Money received from a customer. Kept separate from the sale it settles so one payment can
    // later be allocated across several sales via SaleCustomerPayment.
    public class CustomerPayment : BaseEntity
    {
        public int CustomerId { get; set; } //FK
        public int BranchId { get; set; } //FK
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, Cheque, Credit Card, Other
        public string? Remarks { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SaleCustomerPayment> SaleCustomerPayments { get; set; } = [];
    }
}
