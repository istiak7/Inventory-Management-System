namespace Inventory_Management_System.Entities
{
    // One cross-ledger settlement for a party that is both our customer and our supplier.
    // What they owe us and what we owe them cancel each other out by Amount, so no cash moves.
    //
    // It is written as two payments with method "Adjustment": a CustomerPayment that pays
    // their open sales and a SupplierPayment that pays our open bills, both for Amount.
    // Because they are ordinary payments, invoice dues, statements and payment history all
    // keep working; only cash reports must leave "Adjustment" out.
    //
    // A reversal (when a settled sale is voided) is another row with a negative Amount and
    // ReversalOfId pointing at the settlement it undoes.
    public class PartySettlement : BaseEntity
    {
        public const string PaymentMethod = "Adjustment";

        public int CustomerId { get; set; }
        public int SupplierId { get; set; }
        public int CustomerPaymentId { get; set; }
        public int SupplierPaymentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime SettlementDate { get; set; } = DateTime.UtcNow;

        // The document that started it: a credit sale or a credit purchase.
        public int? SourceSaleId { get; set; }
        public int? SourcePurchaseId { get; set; }
        public int? ReversalOfId { get; set; }

        // Navigation properties
        public required Customer Customer { get; set; }
        public required Supplier Supplier { get; set; }
        public required CustomerPayment CustomerPayment { get; set; }
        public required SupplierPayment SupplierPayment { get; set; }
        public CustomerSale? SourceSale { get; set; }
        public SupplierPurchase? SourcePurchase { get; set; }
        public PartySettlement? ReversalOf { get; set; }
    }
}
