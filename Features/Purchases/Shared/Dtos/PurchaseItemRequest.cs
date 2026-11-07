namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public class PurchaseItemRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarrantyDays { get; set; }
        // One serial per unit, entered when the order is raised. Required (exactly Quantity of
        // them) for a serialized variant; must be empty for a non-serialized one.
        public List<string>? SerialNumbers { get; set; }
    }
}
