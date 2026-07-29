using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class SupplierPurchase : BaseEntity
    {
        public int SupplierId { get; set; } //FK
        public int BranchId { get; set; } //FK
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        public string? InvoiceNumber { get; set; }
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;  // receipt lifecycle, NOT payment state
        public PurchaseType PurchaseType { get; set; } = PurchaseType.Credit;  // created as Credit; paid after approval
        public decimal TotalAmount { get; set; }   // invoice total = sum of the detail lines
        public decimal PaidAmount { get; set; }     // settled so far
        public decimal DueAmount { get; set; }      // outstanding = TotalAmount - PaidAmount

        // Navigation property
        public required Supplier Supplier { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SupplierPurchasePayment> SupplierPurchasePayments { get; set; } = [];

        /// <summary>
        /// Settle <paramref name="amount"/> against this purchase, keeping PaidAmount/DueAmount
        /// consistent in one place. Receipt <see cref="Status"/> is intentionally NOT touched —
        /// payment state (Unpaid/Partial/Paid) is derived from Paid/Due by readers. Throws if the
        /// amount is non-positive or exceeds the current outstanding due.
        /// </summary>
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
