namespace Inventory_Management_System.Entities
{
    // Sales-side counterpart of Supplier. Unlike Supplier, Email is NOT unique/required —
    // retail walk-in customers frequently have no email, and a unique index would make the
    // common "no email" case unsaveable beyond the first row. Phone is the practical handle.
    public class Customer : BaseEntity
    {
        public string Group { get; set; } = string.Empty;
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string PhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NID { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }

        // Navigation property
        public ICollection<CustomerSale> CustomerSales { get; set; } = [];
        public ICollection<CustomerPayment> CustomerPayments { get; set; } = [];
        public ICollection<CustomerTransaction> CustomerTransactions { get; set; } = [];
    }
}
