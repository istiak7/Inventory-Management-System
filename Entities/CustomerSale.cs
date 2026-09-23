using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class CustomerSale : BaseEntity
    {
        public int CustomerId { get; set; } 
        public int BranchId { get; set; } 
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public required string InvoiceNumber { get; set; } 
        public SaleStatus Status { get; set; } = SaleStatus.Completed;
        public SaleType SaleType { get; set; } = SaleType.Cash;   
        public decimal SubTotal { get; set; }         
        public decimal DiscountAmount { get; set; } 
        public decimal TaxAmount { get; set; }      
        public decimal TotalAmount { get; set; }     
        public decimal PaidAmount { get; set; }  
        public decimal DueAmount { get; set; }  
        public string? Remarks { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SaleDetails> SaleDetails { get; set; } = [];
        public ICollection<SaleCustomerPayment> SaleCustomerPayments { get; set; } = [];

        public void ApplyPayment(decimal amount)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Payment amount must be positive.");
            if (amount > DueAmount)
                throw new InvalidOperationException($"Payment {amount} exceeds the outstanding due {DueAmount}.");

            PaidAmount += amount;
            DueAmount -= amount;
        }
    }
}
