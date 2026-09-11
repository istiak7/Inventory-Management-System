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

    public enum PurchaseType
    {
        Cash,
        Credit
    }

    public enum SaleStatus
    {
        Completed
    }

    public enum SaleLineStatus
    {
        Completed
    }

    // How a sale is settled. Cash is paid in full at the point of sale; Debit books the whole
    // amount onto the customer account to be collected later.
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
