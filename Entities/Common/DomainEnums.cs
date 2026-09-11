namespace Inventory_Management_System.Entities.Common
{
    public enum PurchaseStatus
    {
        Pending,
        PartiallyReceived,
        Approved,
        Rejected
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

    public enum SaleStatus
    {
        Completed
    }

    public enum SaleLineStatus
    {
        Completed
    }

    public enum SaleType
    {
        Cash,
        Credit
    }

    public enum SerialStatus
    {
        InStock,
        Sold,
        RmaReturned,
        Defective
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

    public enum TransferStatus
    {
        Draft,
        Pending,
        Approved,
        Rejected
    }
}
