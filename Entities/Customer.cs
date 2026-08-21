namespace Inventory_Management_System.Entities
{
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
