namespace Inventory_Management_System.Entities
{
    public class CustomerTransaction : BaseEntity, Common.ILedgerEntry
    {
        public int CustomerId { get; set; } 
        public string TransactionType { get; set; } = "Sale"; 
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
