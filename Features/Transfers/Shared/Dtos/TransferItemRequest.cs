namespace Inventory_Management_System.Features.Transfers.Shared.Dtos
{
    // Exactly one of Quantity / SerialNumbers is meaningful, decided by ProductVariant.IsSerialized
    // (mirrors SaleItemRequest / ReceiveLineRequest): pooled stock states a plain count, a
    // serialized variant names the physical units and the serial COUNT is the quantity.
    public class TransferItemRequest
    {
        public int ProductVariantId { get; set; }
        public int? Quantity { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }
}
