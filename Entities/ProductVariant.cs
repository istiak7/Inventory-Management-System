namespace Inventory_Management_System.Entities
{
    // The actual purchasable / sellable / stockable unit. Purchasing, stock, serials and
    // inventory always reference ProductVariantId — never ProductId. Cost and warranty are
    // NEVER stored here; they live per purchase line ("lot"), since the same variant can be
    // bought at different costs/terms over time.
    public class ProductVariant : BaseEntity
    {
        public int ProductId { get; set; } //FK
        public required string SKU { get; set; }              // globally unique (unique index)
        public string Barcode { get; set; } = string.Empty;
        public decimal SellingPrice { get; set; }             // catalog price, independent of purchase cost
        public bool IsSerialized { get; set; } = false;
        public string AttributesJson { get; set; } = "{}";    // jsonb (+ GIN index); valid JSON, defaults to "{}"

        // Navigation properties
        public required Product Product { get; set; }
        public ICollection<Stock> Stocks { get; set; } = [];
        public ICollection<SupplierPurchaseDetails> SupplierPurchaseDetails { get; set; } = [];
        public ICollection<SaleDetails> SaleDetails { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
        public ICollection<ProductSerial> ProductSerials { get; set; } = [];
    }
}
