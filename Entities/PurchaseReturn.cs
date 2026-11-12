using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // Goods we send back to a supplier from one approved purchase: some lines, some quantity.
    //
    // The bill keeps its TotalAmount; SupplierPurchase.ReturnedAmount grows instead, so
    // Total = Paid + Due + Returned always holds. TotalAmount here is split three ways:
    //   DueReduced          - taken off what we still owed on the bill
    //   SettlementReversed  - part that had been paid by a cross-ledger Adjustment: that
    //                         settlement is undone (the party owes us as a customer again)
    //   CashRefunded        - money the supplier pays back (a negative "Refund" payment)
    //
    // Cancelling marks it Cancelled, takes the stock back in, and makes TotalAmount due on
    // the bill again. The refund / settlement reversal stay as their own history.
    public class PurchaseReturn : BaseEntity
    {
        public required string ReturnNumber { get; set; }
        public int PurchaseId { get; set; }
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
        public ReturnStatus Status { get; set; } = ReturnStatus.Completed;

        // Value of the returned lines at the bill price, less their share of the bill discount.
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal DueReduced { get; set; }
        public decimal SettlementReversed { get; set; }
        public decimal CashRefunded { get; set; }

        public required string Reason { get; set; }
        public string? Remarks { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }

        // Navigation properties
        public required SupplierPurchase SupplierPurchase { get; set; }
        public required Supplier Supplier { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<PurchaseReturnLine> Lines { get; set; } = [];
    }

    public class PurchaseReturnLine : BaseEntity
    {
        public int PurchaseReturnId { get; set; }
        public int SupplierPurchaseDetailsId { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        // This line's share of the return total (after the bill discount).
        public decimal Amount { get; set; }

        // Navigation properties
        public required PurchaseReturn PurchaseReturn { get; set; }
        public required SupplierPurchaseDetails SupplierPurchaseDetails { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        // Serial numbers sent back (serialized lines only).
        public ICollection<PurchaseReturnSerial> Serials { get; set; } = [];
    }

    public class PurchaseReturnSerial : BaseEntity
    {
        public int PurchaseReturnLineId { get; set; }
        public int ProductSerialId { get; set; }

        public required PurchaseReturnLine PurchaseReturnLine { get; set; }
        public required ProductSerial ProductSerial { get; set; }
    }
}
