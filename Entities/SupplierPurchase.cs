using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class SupplierPurchase : BaseEntity
    {
        public int SupplierId { get; set; } 
        public int BranchId { get; set; } 
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string InvoiceNumber { get; set; } = string.Empty;
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;  
        public PurchaseType? PurchaseType { get; set; }
        public decimal TotalAmount { get; set; }   
        public decimal PaidAmount { get; set; }  
        public decimal DueAmount { get; set; }      
        public string ? Remarks { get; set; } = string.Empty;

        // Navigation property
        public required Supplier Supplier { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SupplierPurchasePayment> SupplierPurchasePayments { get; set; } = [];

        // An approved order is complete: the goods are in stock and the supplier ledger has
        // been posted, so it can no longer be edited or deleted.
        public bool IsCompleted => Status == PurchaseStatus.Approved;

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
