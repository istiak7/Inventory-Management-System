namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    // One order line at PO creation. Targets a ProductVariant (never a Product). Only the
    // ordered quantity is known here — received quantity/serials come later at goods receipt.
    public class PurchaseItemRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }          // ordered quantity
        public decimal UnitPrice { get; set; }     // this lot's purchase cost
        public int WarrantyMonths { get; set; }    // this lot's warranty term
    }
}
