using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class SupplierPurchase : BaseEntity
    {
        public int SupplierId { get; set; } 
        public int BranchId { get; set; } 
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public string InvoiceNumber { get; set; } = string.Empty;
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;  
        public PurchaseType PurchaseType { get; set; } = PurchaseType.Credit;  
        public decimal TotalAmount { get; set; }   
        public decimal PaidAmount { get; set; }  
        public decimal DueAmount { get; set; }      
        public string ? Remarks { get; set; } = string.Empty;

        // Navigation property
        public required Supplier Supplier { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SupplierPurchasePayment> SupplierPurchasePayments { get; set; } = [];

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
