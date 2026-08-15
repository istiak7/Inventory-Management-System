namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    // One sale line. Targets a ProductVariant (never a Product).
    public class SaleItemRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }

        // The price actually agreed with the customer for this line. Optional: leave it null and
        // the server falls back to ProductVariant.SellingPrice (the catalog/default price). What
        // is sent here is stored on the sale line only — it never edits the catalog price.
        public decimal? UnitPrice { get; set; }

        public decimal? DiscountPerItem { get; set; }   // per-unit discount off the unit price
        public int? WarrantyMonths { get; set; }        // warranty sold with this line

        // Required when the variant is serialized (ProductVariant.IsSerialized); identifies the
        // exact physical unit being sold. Ignored for non-serialized variants.
        public string? SerialNumber { get; set; }
    }
}
