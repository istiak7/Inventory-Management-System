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
    /// <summary>
    /// Lifecycle of an after-sales warranty claim. Open is intake at the counter; InRepair is a
    /// technician holding the unit; Resolved and Rejected are the two ways the shop is done with
    /// it; Delivered is the customer physically collecting it. Delivered is deliberately reachable
    /// from BOTH Resolved and Rejected — a unit the shop refused to fix still has to go home.
    /// </summary>
    public enum WarrantyClaimStatus
    {
        Open,
        InRepair,
        Resolved,
        Rejected,
        Delivered
    }

    /// <summary>
    /// HOW a claim ended, kept separate from <see cref="WarrantyClaimStatus"/> so "Resolved by
    /// replacing the unit" is one state instead of two competing ones. NotRepairable is what the
    /// reject path records.
    /// </summary>
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

    /// <summary>
    /// Lifecycle of a branch-to-branch stock transfer. Mirrors <see cref="PurchaseStatus"/>'s
    /// request/decision shape: Draft/Pending are both "not yet decided" and can be Approved or
    /// Rejected; stock only actually moves at Approved (there is no separate receive step today —
    /// approval IS the transfer, same "no pending delivery stage" style as <see cref="SaleStatus"/>).
    /// </summary>
    public enum TransferStatus
    {
        Draft,
        Pending,
        Approved,
        Rejected
    }
}
