namespace Inventory_Management_System.Features.Purchases.Shared.Dtos
{
    public sealed record PurchaseOrderDetailLineResponse(
        int Id,
        int ProductVariantId,
        string SKU,
        string ProductName,
        bool IsSerialized,
        int OrderedQuantity,
        int? ReceivedQuantity,
        decimal UnitPrice,
        decimal TotalAmount,
        int WarrantyDays,
        string Status,
        // Serials received into stock against this line.
        IReadOnlyList<string> SerialNumbers,
        // Serials entered on the order that have not been received yet.
        IReadOnlyList<string> PendingSerialNumbers,
        // Units already sent back to the supplier by purchase returns.
        int ReturnedQuantity = 0,
        // Every received serial with its status, so an edit can tell which ones are still free to change.
        IReadOnlyList<PurchaseSerialState>? SerialStates = null
    );

    public sealed record PurchaseSerialState(string SerialNumber, string Status);

    public sealed record PurchaseOrderDetailResponse(
        int Id,
        int SupplierId,
        string SupplierName,
        int BranchId,
        string BranchName,
        string? InvoiceNumber,
        string? Remarks,
        DateTime PurchaseDate,
        string Status,
        string? PurchaseType,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal DueAmount,
        decimal DiscountAmount,
        IReadOnlyList<PurchaseOrderDetailLineResponse> Items,
        // Value of goods sent back to the supplier by purchase returns (TotalAmount itself stays).
        decimal ReturnedAmount = 0,
        // Paid money released from this bill by earlier edits and still free (our advance with the supplier).
        decimal ReleasedPayment = 0,
        // Net money actually paid to the supplier on this bill (cash, less refunds); settlements excluded.
        decimal CashPaidAmount = 0
    );
}
