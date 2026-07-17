namespace Inventory_Management_System.Entities
{
    public class Supplier : BaseEntity
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string NID { get; set; }
        public int OpeningBalance { get; set; }

        // Navigation property
        public ICollection<SupplierPurchase> SupplierPurchases { get; set; } = [];
        public ICollection<SupplierPayment> SupplierPayments { get; set; } = [];
        public ICollection<SupplierTransaction> SupplierTransactions { get; set; } = [];
    }
}
