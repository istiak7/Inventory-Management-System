namespace Inventory_Management_System.Entities
{
    public class Supplier : BaseEntity
    {
        public string Group { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string PhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NID { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }

        // Navigation property
        public ICollection<SupplierPurchase> SupplierPurchases { get; set; } = [];
        public ICollection<SupplierPayment> SupplierPayments { get; set; } = [];
        public ICollection<SupplierTransaction> SupplierTransactions { get; set; } = [];
    }
}
