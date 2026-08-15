using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // Sale header — this row IS the invoice (POS/Approach 1: no separate pending or delivery
    // stage). Mirrors SupplierPurchase on the sales side, with the money fields extended for
    // header-level discount/tax.
    public class CustomerSale : BaseEntity
    {
        public int CustomerId { get; set; } //FK
        public int BranchId { get; set; } //FK
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public required string InvoiceNumber { get; set; }   // unique; server-generated when omitted
        public SaleStatus Status { get; set; } = SaleStatus.Completed;
        public SaleType SaleType { get; set; } = SaleType.Cash;   // Cash = fully paid at creation; else Credit
        public decimal SubTotal { get; set; }         // sum of the detail lines, before header discount/tax
        public decimal DiscountAmount { get; set; }   // header-level discount
        public decimal TaxAmount { get; set; }        // header-level tax
        public decimal TotalAmount { get; set; }      // SubTotal - DiscountAmount + TaxAmount
        public decimal PaidAmount { get; set; }       // settled so far
        public decimal DueAmount { get; set; }        // outstanding = TotalAmount - PaidAmount

        // Free-text note about this sale, written at the counter and printed on the invoice.
        // Same field as SupplierPurchase.Remarks on the purchase side.
        public string? Remarks { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SaleDetails> SaleDetails { get; set; } = [];
        public ICollection<SaleCustomerPayment> SaleCustomerPayments { get; set; } = [];

        /// <summary>
        /// Settle <paramref name="amount"/> against this sale, keeping PaidAmount/DueAmount
        /// consistent in one place. <see cref="Status"/> is intentionally NOT touched — payment
        /// state (Unpaid/Partial/Paid) is derived from Paid/Due by readers. Throws if the amount
        /// is non-positive or exceeds the current outstanding due.
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
