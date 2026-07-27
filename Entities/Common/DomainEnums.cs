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

    public enum SerialStatus
    {
        InStock,
        Sold,
        RmaReturned,
        Defective
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
}
