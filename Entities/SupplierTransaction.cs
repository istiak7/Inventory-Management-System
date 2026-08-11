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
