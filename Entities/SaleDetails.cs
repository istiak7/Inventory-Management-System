using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // One sale line. UnitPrice is snapshotted from ProductVariant.SellingPrice at sale time so
    // a later catalog price change never rewrites history; it is NEVER taken from the client.
    public class SaleDetails : BaseEntity
    {
        public int SaleId { get; set; } //FK
        public int ProductVariantId { get; set; } //FK
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }            // server-resolved selling price at sale time
        public decimal? DiscountPerItem { get; set; }     // per-unit discount off UnitPrice
        public decimal TotalAmount { get; set; }          // line total = (UnitPrice - DiscountPerItem) * Quantity
        public int? WarrantyMonths { get; set; }          // warranty sold with this line

        public int? ProductSerialId { get; set; } //FK, for serialized products only; null for non-serialized products
        public SaleLineStatus Status { get; set; } = SaleLineStatus.Completed;

        // Navigation property
        public required CustomerSale CustomerSale { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public ProductSerial? ProductSerial { get; set; }
    }
}
