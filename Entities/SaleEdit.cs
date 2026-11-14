namespace Inventory_Management_System.Entities
{
    // Audit record of one edit of a completed sale. The sale row always shows the current
    // values; this row keeps what it was before, what it became, and what money / stock /
    // settlement movements the edit caused (each of those also has its own ledger, payment,
    // settlement and inventory rows, so nothing is rewritten silently). Who edited and when are
    // CreatedById / CreatedAt.
    public class SaleEdit : BaseEntity
    {
        public int SaleId { get; set; }
        public required string Reason { get; set; }

        public decimal OldSubTotal { get; set; }
        public decimal OldDiscountAmount { get; set; }
        public decimal OldTaxAmount { get; set; }
        public decimal OldTotalAmount { get; set; }
        public decimal OldPaidAmount { get; set; }
        public decimal OldDueAmount { get; set; }

        public decimal NewSubTotal { get; set; }
        public decimal NewDiscountAmount { get; set; }
        public decimal NewTaxAmount { get; set; }
        public decimal NewTotalAmount { get; set; }
        public decimal NewPaidAmount { get; set; }
        public decimal NewDueAmount { get; set; }

        // Value of goods already returned when the edit was made (it is left untouched).
        public decimal ReturnedAmount { get; set; }

        // Cross-ledger settlement undone on this sale, and what was settled again afterwards.
        public decimal SettlementReversed { get; set; }
        public decimal SettlementRedone { get; set; }
        // Paid amount no longer needed by the sale: kept as the customer's advance (CashReleased)
        // or handed back in cash (CashRefunded, a subset chosen by the editor).
        public decimal CashReleased { get; set; }
        public decimal CashRefunded { get; set; }
        // Earlier payments of this sale put back on it, and new money collected with the edit.
        public decimal CashReapplied { get; set; }
        public decimal AdditionalPayment { get; set; }

        // JSON: the lines before and after, and the stock adjustments made.
        public string ChangesJson { get; set; } = "{}";

        public CustomerSale CustomerSale { get; set; } = null!;
    }
}
