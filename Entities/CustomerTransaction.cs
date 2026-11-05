namespace Inventory_Management_System.Entities
{
    public class CustomerTransaction : BaseEntity, Common.ILedgerEntry
    {
        public int CustomerId { get; set; } 
        public string TransactionType { get; set; } = "Sale"; 
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal BalanceAfter { get; set; }

        public int? SaleId { get; set; }
        public int? CustomerPaymentId { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public CustomerSale? CustomerSale { get; set; }
        public CustomerPayment? CustomerPayment { get; set; }

        // What the customer already owed before using this system. First row of their ledger.
        public static CustomerTransaction ForOpening(Customer customer)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Opening",
                TransactionDate = customer.CreatedAt,
                Debit = 0m,
                Credit = customer.OpeningBalance,
                BalanceAfter = customer.OpeningBalance,
                Customer = customer
            };
        }

        public static CustomerTransaction ForSale(CustomerSale sale, Customer customer, decimal runningBalance)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Sale",
                TransactionDate = sale.SaleDate,
                Debit = 0m,
                Credit = sale.TotalAmount,
                BalanceAfter = runningBalance,
                Customer = customer,
                CustomerSale = sale
            };
        }

        // A discount given while taking a payment: we waive part of what the customer owes, so it
        // lowers the receivable balance just like a payment does (a Debit), but it is not money received.
        public static CustomerTransaction ForDiscount(CustomerPayment payment, Customer customer, decimal amount, decimal runningBalance)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Discount",
                TransactionDate = payment.PaymentDate,
                Debit = amount,
                Credit = 0m,
                BalanceAfter = runningBalance,
                Customer = customer,
                CustomerPayment = payment
            };
        }

        // A voided sale: takes the sale's (net) amount back off what the customer owes.
        public static CustomerTransaction ForVoid(CustomerSale sale, Customer customer, DateTime voidDate, decimal runningBalance)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Void",
                TransactionDate = voidDate,
                Debit = sale.TotalAmount,
                Credit = 0m,
                BalanceAfter = runningBalance,
                Customer = customer,
                CustomerSale = sale
            };
        }

        // Money handed back for a voided sale: undoes the payments the sale had received.
        public static CustomerTransaction ForRefund(CustomerPayment refund, Customer customer, decimal amount, decimal runningBalance, CustomerSale sale)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Refund",
                TransactionDate = refund.PaymentDate,
                Debit = 0m,
                Credit = amount,
                BalanceAfter = runningBalance,
                Customer = customer,
                CustomerPayment = refund,
                CustomerSale = sale
            };
        }

        public static CustomerTransaction ForPayment(CustomerPayment payment, Customer customer, decimal runningBalance, CustomerSale? sale = null)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Payment",
                TransactionDate = payment.PaymentDate,
                Debit = payment.Amount,
                Credit = 0m,
                BalanceAfter = runningBalance,
                Customer = customer,
                CustomerPayment = payment,
                CustomerSale = sale
            };
        }
    }
}
