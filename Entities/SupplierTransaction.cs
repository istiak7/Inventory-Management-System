namespace Inventory_Management_System.Entities
{
    public class SupplierTransaction : BaseEntity, Common.ILedgerEntry
    {
        public int SupplierId { get; set; }
        public string TransactionType { get; set; } = "Purchase";
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal BalanceAfter { get; set; }  // added — running balance, computed once at insert time

        public int? SupplierPurchaseId { get; set; }
        public int? SupplierPaymentId { get; set; }

        // Navigation property
        public required Supplier Supplier { get; set; }
        public SupplierPurchase? SupplierPurchase { get; set; }
        public SupplierPayment? SupplierPayment { get; set; }
    }
}
