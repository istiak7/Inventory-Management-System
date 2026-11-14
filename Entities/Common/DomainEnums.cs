namespace Inventory_Management_System.Entities.Common
{
    public enum PurchaseStatus
    {
        Pending,
        PartiallyReceived,
        Approved,
        Rejected,
        // An approved purchase that was cancelled afterwards: its stock was taken back out and its
        // payable reversed, the record stays (see CancelPurchaseOrder).
        Cancelled
    }

    public enum LineStatus
    {
        Pending,
        PartiallyReceived,
        Received,
        Rejected
    }

    // How a purchase is settled. Cash is paid in full the moment the order is approved;
    // Debit books the whole amount onto the supplier account to be paid later.
    public enum PurchaseType
    {
        Cash,
        Debit
    }

    // Voided = the sale was cancelled after the fact: its stock came back, its serials are
    // sellable again and any money taken was refunded. The record stays for audit.
    public enum SaleStatus
    {
        Completed,
        Voided
    }

    public enum SaleLineStatus
    {
        Completed,
        Voided
    }

    // How a sale is settled. Cash is paid in full at the point of sale; Debit books the whole
    // amount onto the customer account to be collected later.
    // A sales or purchase return. Cancelled = undone; kept for the record.
    public enum ReturnStatus
    {
        Completed,
        Cancelled
    }

    public enum SaleType
    {
        Cash,
        Debit
    }

    public enum SerialStatus
    {
        InStock,
        Sold,
        RmaReturned,
        Defective,
        // Serial entered on a purchase order that has not been received yet. It reserves the
        // number (the unique index) but is not stock: approval turns it into InStock.
        Ordered
    }

    public enum WarrantyClaimStatus
    {
        Open,
        InRepair,
        Resolved,
        Rejected,
        Delivered
    }

    public enum WarrantyResolutionType
    {
        Repaired,
        Replaced,
        NotRepairable
    }

    public enum InventoryTxnType
    {
        PurchaseIn,
        SaleOut,
        ReturnIn,
        ReturnOut,
        TransferIn,
        TransferOut,
        Adjustment,
        DamageOut
    }

    // Draft and Sent are open offers. Once ValidUntil has passed they read as Expired (see
    // QuotationRules.EffectiveStatus) without a job having to update them. Converted is set
    // only by turning an Accepted quotation into a sale, and is final.
    public enum QuotationStatus
    {
        Draft,
        Sent,
        Accepted,
        Rejected,
        Expired,
        Converted
    }

    public enum TransferStatus
    {
        Draft,
        Pending,
        Approved,
        Rejected
    }
}
