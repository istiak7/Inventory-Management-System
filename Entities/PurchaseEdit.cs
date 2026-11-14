namespace Inventory_Management_System.Entities
{
    // Audit record of one edit of an approved purchase, the counterpart of SaleEdit. The purchase
    // always shows its current values; this row keeps what it was, what it became and what money,
    // stock and settlement movements the edit caused (each also has its own ledger, payment,
    // settlement and inventory rows). Who edited and when are CreatedById / CreatedAt.
    public class PurchaseEdit : BaseEntity
    {
        public int PurchaseId { get; set; }
        public required string Reason { get; set; }

        public decimal OldDiscountAmount { get; set; }
        public decimal OldTotalAmount { get; set; }
        public decimal OldPaidAmount { get; set; }
        public decimal OldDueAmount { get; set; }

        public decimal NewDiscountAmount { get; set; }
        public decimal NewTotalAmount { get; set; }
        public decimal NewPaidAmount { get; set; }
        public decimal NewDueAmount { get; set; }

        // Value of goods already returned to the supplier when the edit was made (left untouched).
        public decimal ReturnedAmount { get; set; }

        // Cross-ledger settlement undone on this bill, and what was settled again afterwards.
        public decimal SettlementReversed { get; set; }
        public decimal SettlementRedone { get; set; }
        // Paid amount the bill no longer needs: kept as our advance with the supplier
        // (CashReleased) or paid back to us in cash (CashRefunded, chosen by the editor).
        public decimal CashReleased { get; set; }
        public decimal CashRefunded { get; set; }
        // Earlier payments of this bill put back on it, and new money paid with the edit.
        public decimal CashReapplied { get; set; }
        public decimal AdditionalPayment { get; set; }

        // JSON: the lines before and after, and the stock adjustments made.
        public string ChangesJson { get; set; } = "{}";

        public SupplierPurchase SupplierPurchase { get; set; } = null!;
    }
}
