namespace Inventory_Management_System.Features.Sales.Shared.Dtos
{
    // One sale line. Targets a ProductVariant (never a Product). There is deliberately NO
    // UnitPrice here: the server resolves it from ProductVariant.SellingPrice so a client
    // cannot dictate what it pays. Only the discount is negotiable from the client side.
    public class SaleItemRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal? DiscountPerItem { get; set; }   // per-unit discount off the selling price
        public int? WarrantyMonths { get; set; }        // warranty sold with this line
    }
}
