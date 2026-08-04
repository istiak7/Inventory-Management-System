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

    // Payment settled against the supplier at goods receipt. Only takes effect once every
    // non-rejected line is Received (the order becomes Approved) — that's the moment the
    // purchase debit is posted to the supplier ledger, so any payment must land in the same
    // transaction as that debit. Mode is inferred from Amount vs the order total, same as sales:
    //   Amount omitted / 0   -> full due (nothing paid)
    //   0 < Amount < Total   -> partial payment
    //   Amount == Total      -> full payment
    //   Amount > Total       -> rejected
    public class ReceiveGoodsPaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, Cheque, Credit Card, Other
    }

    public sealed record ReceivedLineResponse(
        int SupplierPurchaseDetailsId,
        int OrderedQuantity,
        int ReceivedQuantity,
        string Status);

    public sealed record ReceiveGoodsPaymentResponse(
        int Id,
        decimal Amount,
        DateTime PaymentDate,
        string PaymentMethod);

    public sealed record ReceiveGoodsResponse(
        int PurchaseOrderId,
        string Status,
        int SerialsCreated,
        decimal PaidAmount,
        decimal DueAmount,
        IReadOnlyList<ReceivedLineResponse> Lines,
        ReceiveGoodsPaymentResponse? Payment);
}
