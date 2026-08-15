using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // One sale line. UnitPrice is the price actually sold at, snapshotted here at sale time: the
    // seller may keep the variant's catalog price (the default) or agree a different one. Either
    // way it lives on this line only, so a later catalog price change never rewrites history and
    // editing a sale price never changes ProductVariant.SellingPrice.
    public class SaleDetails : BaseEntity
    {
        public int SaleId { get; set; } //FK
        public int ProductVariantId { get; set; } //FK
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }            // price sold at (default: ProductVariant.SellingPrice)
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
