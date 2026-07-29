namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    // One receipt line. For a serialized variant, SerialNumbers is required and its count
    // IS the received quantity. For a non-serialized variant, ReceivedQuantity is used.
    public class ReceiveLineRequest
    {
        public int SupplierPurchaseDetailsId { get; set; }
        public int? ReceivedQuantity { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }

    public sealed record ReceivedLineResponse(
        int SupplierPurchaseDetailsId,
        int OrderedQuantity,
        int ReceivedQuantity,
        string Status);

    public sealed record ReceiveGoodsResponse(
        int PurchaseOrderId,
        string Status,
        int SerialsCreated,
        IReadOnlyList<ReceivedLineResponse> Lines);
}
