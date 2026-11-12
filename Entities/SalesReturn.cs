using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // Goods a customer brings back from one sale: some lines, some quantity of each.
    //
    // The sale keeps its TotalAmount; CustomerSale.ReturnedAmount grows instead, so
    // Total = Paid + Due + Returned always holds. TotalAmount here is split three ways:
    //   DueReduced          - taken off what was still owed on the sale
    //   SettlementReversed  - part that had been paid by a cross-ledger Adjustment: that
    //                         settlement is undone (we owe the party as a supplier again)
    //   CashRefunded        - handed back in cash (a negative "Refund" payment)
    //
    // Cancelling marks it Cancelled, puts stock and serials back, and makes TotalAmount due
    // on the sale again. The refund / settlement reversal stay as their own history.
    public class SalesReturn : BaseEntity
    {
        public required string ReturnNumber { get; set; }
        public int SaleId { get; set; }
        public int CustomerId { get; set; }
        public int BranchId { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;
        public ReturnStatus Status { get; set; } = ReturnStatus.Completed;

        // Value of the returned lines at the sale price, and their share of the sale's
        // whole-invoice discount and tax. TotalAmount = SubTotal - DiscountAmount + TaxAmount.
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public decimal DueReduced { get; set; }
        public decimal SettlementReversed { get; set; }
        public decimal CashRefunded { get; set; }

        public required string Reason { get; set; }
        public string? Remarks { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }

        // Navigation properties
        public required CustomerSale CustomerSale { get; set; }
        public required Customer Customer { get; set; }
        public required Branch Branch { get; set; }
        public ICollection<SalesReturnLine> Lines { get; set; } = [];
    }

    public class SalesReturnLine : BaseEntity
    {
        public int SalesReturnId { get; set; }
        public int SaleDetailsId { get; set; }
        public int ProductVariantId { get; set; }
        public int? ProductSerialId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        // This line's share of the return total (after discount, with tax).
        public decimal Amount { get; set; }
        // Cost snapshot from the sale line, so profit reports can take the cost back out.
        public decimal UnitCost { get; set; }
        // True = back into sellable stock. False = damaged: not restocked (serial -> Defective).
        public bool Restock { get; set; } = true;

        // Navigation properties
        public required SalesReturn SalesReturn { get; set; }
        public required SaleDetails SaleDetails { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public ProductSerial? ProductSerial { get; set; }
    }
}
