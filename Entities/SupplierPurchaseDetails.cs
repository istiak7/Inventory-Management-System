namespace Inventory_Management_System.Entities
{
    public class SupplierPurchaseDetails : BaseEntity
    {
        public int PurchaseId { get; set; } //FK
        public int SKU { get; set; } //FK
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public string IsApproved { get; set; } = "Pending"; // Approved, Rejected, Pending

        // Navigation property
        public required SupplierPurchase SupplierPurchase { get; set; }
        public required Product Product { get; set; }
    }
}
