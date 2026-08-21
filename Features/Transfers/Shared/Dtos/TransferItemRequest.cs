namespace Inventory_Management_System.Features.Transfers.Shared.Dtos
{
    public class TransferItemRequest
    {
        public int ProductVariantId { get; set; }
        public int? Quantity { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }
}
