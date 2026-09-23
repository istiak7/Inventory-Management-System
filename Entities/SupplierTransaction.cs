namespace Inventory_Management_System.Entities
{
    public class SupplierTransaction : BaseEntity, Common.ILedgerEntry
    {
        public int SupplierId { get; set; }
        public string TransactionType { get; set; } = "Purchase";
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal BalanceAfter { get; set; }  

        public int? SupplierPurchaseId { get; set; }
        public int? SupplierPaymentId { get; set; }

        // Navigation property
        public required Supplier Supplier { get; set; }
        public SupplierPurchase? SupplierPurchase { get; set; }
        public SupplierPayment? SupplierPayment { get; set; }


        // What we already owed the supplier before using this system. First row of their ledger.
        public static SupplierTransaction ForOpening(Supplier supplier)
        {
            return new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionType = "Opening",
                TransactionDate = supplier.CreatedAt,
                Debit = supplier.OpeningBalance,
                Credit = 0m,
                BalanceAfter = supplier.OpeningBalance,
                Supplier = supplier
            };
        }

        public static SupplierTransaction ForPurchase(SupplierPurchase purchase, Supplier supplier, decimal runningBalance)
        {
            return new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionType = "Purchase",
                TransactionDate = purchase.PurchaseDate,
                Debit = purchase.TotalAmount,
                Credit = 0m,
                BalanceAfter = runningBalance,
                Supplier = supplier,
                SupplierPurchase = purchase
            };
        }

        public static SupplierTransaction ForPayment(SupplierPayment payment, Supplier supplier, decimal runningBalance, SupplierPurchase? purchase = null)
        {
            return new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionType = "Payment",
                TransactionDate = payment.PaymentDate,
                Debit = 0m,
                Credit = payment.Amount,
                BalanceAfter = runningBalance,
                Supplier = supplier,
                SupplierPayment = payment,
                SupplierPurchase = purchase
            };
        }
    }
}
