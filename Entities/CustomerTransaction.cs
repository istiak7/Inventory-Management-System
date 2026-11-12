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
        public int? SalesReturnId { get; set; }

        // Navigation property
        public required Customer Customer { get; set; }
        public CustomerSale? CustomerSale { get; set; }
        public CustomerPayment? CustomerPayment { get; set; }
        public SalesReturn? SalesReturn { get; set; }

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

        // A cross-ledger settlement (see PartySettlement): what we owe this party as a supplier
        // pays off what they owe us. A positive amount lowers the receivable like a payment;
        // a negative amount (a reversal, when a settled sale is voided) puts it back.
        public static CustomerTransaction ForAdjustment(CustomerPayment payment, Customer customer, decimal amount, decimal runningBalance, CustomerSale? sale = null)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Adjustment",
                TransactionDate = payment.PaymentDate,
                Debit = amount > 0 ? amount : 0m,
                Credit = amount < 0 ? -amount : 0m,
                BalanceAfter = runningBalance,
                Customer = customer,
                CustomerPayment = payment,
                CustomerSale = sale
            };
        }

        // A sales return: a positive amount takes the returned goods off what the customer owes;
        // a negative amount (the return was cancelled) puts it back.
        public static CustomerTransaction ForReturn(SalesReturn salesReturn, Customer customer, decimal amount, DateTime date, decimal runningBalance)
        {
            return new CustomerTransaction
            {
                CustomerId = customer.Id,
                TransactionType = "Return",
                TransactionDate = date,
                Debit = amount > 0 ? amount : 0m,
                Credit = amount < 0 ? -amount : 0m,
                BalanceAfter = runningBalance,
                Customer = customer,
                CustomerSale = salesReturn.CustomerSale,
                SalesReturn = salesReturn
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
