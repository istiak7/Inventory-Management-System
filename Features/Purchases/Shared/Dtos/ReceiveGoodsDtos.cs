namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public class ReceiveLineRequest
    {
        public int SupplierPurchaseDetailsId { get; set; }
        public int? ReceivedQuantity { get; set; }
        public List<string>? SerialNumbers { get; set; }
    }

    public class ReceiveGoodsPaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }

    public sealed record ReceivedLineResponse(
        int SupplierPurchaseDetailsId,
        int OrderedQuantity,
        int ReceivedQuantity,
        string Status
    );

    public sealed record ReceiveGoodsPaymentResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod
    );

    public sealed record ReceiveGoodsResponse(
        int PurchaseOrderId,
        string Status,
        int SerialsCreated,
        decimal PaidAmount,
        decimal DueAmount,
        IReadOnlyList<ReceivedLineResponse> Lines,
        ReceiveGoodsPaymentResponse? Payment
    );
}
