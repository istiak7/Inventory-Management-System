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

    /// <summary>
    /// Header-level lifecycle of a customer sale. POS-style (Approach 1): the sale and its
    /// invoice are created in the same step, so there is no pending/delivery stage — a sale
    /// that exists is already Completed.
    /// </summary>
    public enum SaleStatus
    {
        Completed
    }

    /// <summary>Per-line status of a sale detail. Mirrors <see cref="SaleStatus"/> today.</summary>
    public enum SaleLineStatus
    {
        Completed
    }

    /// <summary>How the sale is being paid for. Cash = fully paid at creation; otherwise Credit.</summary>
    public enum SaleType
    {
        Cash,
        Credit
    }

    /// <summary>Lifecycle state of a single serialized physical unit.</summary>
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
