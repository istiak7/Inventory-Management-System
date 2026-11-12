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
        public int? PurchaseReturnId { get; set; }

        // Navigation property
        public required Supplier Supplier { get; set; }
        public SupplierPurchase? SupplierPurchase { get; set; }
        public SupplierPayment? SupplierPayment { get; set; }
        public PurchaseReturn? PurchaseReturn { get; set; }


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

        // A discount taken while paying: the supplier waives part of what we owe, so it lowers
        // the payable balance just like a payment does (a Credit), but it is not money paid out.
        public static SupplierTransaction ForDiscount(SupplierPayment payment, Supplier supplier, decimal amount, decimal runningBalance)
        {
            return new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionType = "Discount",
                TransactionDate = payment.PaymentDate,
                Debit = 0m,
                Credit = amount,
                BalanceAfter = runningBalance,
                Supplier = supplier,
                SupplierPayment = payment
            };
        }

        // A cross-ledger settlement (see PartySettlement): what this party owes us as a customer
        // pays off what we owe them. A positive amount lowers the payable like a payment;
        // a negative amount (a reversal, when a settled sale is voided) puts it back.
        public static SupplierTransaction ForAdjustment(SupplierPayment payment, Supplier supplier, decimal amount, decimal runningBalance, SupplierPurchase? purchase = null)
        {
            return new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionType = "Adjustment",
                TransactionDate = payment.PaymentDate,
                Debit = amount < 0 ? -amount : 0m,
                Credit = amount > 0 ? amount : 0m,
                BalanceAfter = runningBalance,
                Supplier = supplier,
                SupplierPayment = payment,
                SupplierPurchase = purchase
            };
        }

        // A purchase return: a positive amount takes the returned goods off what we owe;
        // a negative amount (the return was cancelled) puts it back.
        public static SupplierTransaction ForReturn(PurchaseReturn purchaseReturn, Supplier supplier, decimal amount, DateTime date, decimal runningBalance)
        {
            return new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionType = "Return",
                TransactionDate = date,
                Debit = amount < 0 ? -amount : 0m,
                Credit = amount > 0 ? amount : 0m,
                BalanceAfter = runningBalance,
                Supplier = supplier,
                SupplierPurchase = purchaseReturn.SupplierPurchase,
                PurchaseReturn = purchaseReturn
            };
        }

        // Money the supplier pays back to us for returned goods we had already paid for.
        // It undoes part of our payment, so the payable goes back up (a Debit).
        public static SupplierTransaction ForRefund(SupplierPayment refund, Supplier supplier, decimal amount, decimal runningBalance, SupplierPurchase purchase, PurchaseReturn purchaseReturn)
        {
            return new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionType = "Refund",
                TransactionDate = refund.PaymentDate,
                Debit = amount,
                Credit = 0m,
                BalanceAfter = runningBalance,
                Supplier = supplier,
                SupplierPayment = refund,
                SupplierPurchase = purchase,
                PurchaseReturn = purchaseReturn
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
