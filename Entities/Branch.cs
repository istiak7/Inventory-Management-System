namespace Inventory_Management_System.Entities
{
    public class Branch : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Navigation property
        public ICollection<SupplierPurchase> SupplierPurchases { get; set; } = [];
        public ICollection<CustomerSale> CustomerSales { get; set; } = [];
        public ICollection<CustomerPayment> CustomerPayments { get; set; } = [];
        public ICollection<Stock> Stocks { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
    }
}
