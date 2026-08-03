namespace Inventory_Management_System.Entities
{
    // Append-only customer money ledger (sales-side mirror of SupplierTransaction). BalanceAfter
    // is the customer's running receivable — what they owe us — computed once at insert time:
    //   Sale    -> Credit = total,  balance goes UP   (they owe more)
    //   Payment -> Debit  = amount, balance goes DOWN (they owe less)
    // Note this is the opposite Debit/Credit convention from SupplierTransaction, where the
    // balance tracks what WE owe. Each ledger reads from its own counterparty's perspective.
    public class CustomerTransaction : BaseEntity, Common.ILedgerEntry
    {
        public int CustomerId { get; set; } //FK
        public string TransactionType { get; set; } = "Sale";   // "Sale" | "Payment"
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal BalanceAfter { get; set; }

        public int? SaleId { get; set; }
        public int? CustomerPaymentId { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public CustomerSale? CustomerSale { get; set; }
        public CustomerPayment? CustomerPayment { get; set; }
    }
}
