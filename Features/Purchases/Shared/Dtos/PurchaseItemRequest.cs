namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public class PurchaseItemRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int WarrantyMonths { get; set; }
    }
}
