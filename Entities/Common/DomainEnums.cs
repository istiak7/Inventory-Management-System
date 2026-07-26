namespace Inventory_Management_System.Entities.Common
{
    // All domain enums are persisted as STRINGS (EF HasConversion<string>()), so the DB
    // columns stay human-readable and adding a new member never needs a data migration.

    /// <summary>Header-level receipt lifecycle of a supplier purchase order.</summary>
    public enum PurchaseStatus
    {
        Pending,            // created, nothing received yet
        PartiallyReceived,  // some (but not all) lines/quantities received
        Approved,           // every line fully received
        Rejected            // cancelled before receipt
    }

    /// <summary>Per-line receipt status of a purchase detail (the "lot").</summary>
    public enum LineStatus
    {
        Pending,
        PartiallyReceived,
        Received,
        Rejected
    }

    /// <summary>How the purchase is being paid for. Cash = fully paid at creation; otherwise Credit.</summary>
    public enum PurchaseType
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

    /// <summary>Movement type recorded on the append-only inventory ledger.</summary>
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
