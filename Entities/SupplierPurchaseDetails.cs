using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // One purchase line == one "lot": a specific quantity of a variant bought at a specific
    // UnitPrice with a specific WarrantyMonths term. Cost/warranty live HERE, never on the variant.
    public class SupplierPurchaseDetails : BaseEntity
    {
        public int PurchaseId { get; set; } //FK
        public int ProductVariantId { get; set; } //FK
        public int OrderedQuantity { get; set; }        // known at PO creation
        public int? ReceivedQuantity { get; set; }      // null until first goods receipt; accumulates across receipts
        public decimal UnitPrice { get; set; }          // this lot's purchase cost
        public decimal TotalAmount { get; set; }        // extended line price = OrderedQuantity * UnitPrice
        public int WarrantyMonths { get; set; }         // this lot's warranty term (entered at creation, copied to serials)
        public LineStatus Status { get; set; } = LineStatus.Pending;

        // Navigation property
        public required SupplierPurchase SupplierPurchase { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public ICollection<ProductSerial> ProductSerials { get; set; } = [];

        /// <summary>
        /// Book a received quantity against this lot. Receipts are incremental — this accumulates
        /// onto <see cref="ReceivedQuantity"/> and recomputes <see cref="Status"/>. Over-receipt
        /// (received &gt; ordered) is allowed by business rule and simply lands as Received.
        /// </summary>
        public void ApplyReceipt(int quantity)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Received quantity must be positive.");

            ReceivedQuantity = (ReceivedQuantity ?? 0) + quantity;
            Status = ReceivedQuantity >= OrderedQuantity ? LineStatus.Received : LineStatus.PartiallyReceived;
        }

        public void Reject() => Status = LineStatus.Rejected;
    }
}
