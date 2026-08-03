namespace Inventory_Management_System.Entities
{
    // One line == one product variant moving from the transfer's source to its destination.
    // Quantity is the unit count regardless of serialization; for a serialized variant it always
    // equals RequestedSerialNumbersJson's length, since the serial IS the unit (mirrors SaleDetails).
    public class StockTransferDetails : BaseEntity
    {
        public int StockTransferId { get; set; }   //FK
        public int ProductVariantId { get; set; }  //FK
        public int Quantity { get; set; }

        // Serialized lines only. Captured at creation (Draft/Pending) so the request is a fixed,
        // re-displayable record even before any ProductSerial row is claimed — the actual
        // ProductSerial resolution/claim happens later, at approval. Empty ("[]") for pooled stock.
        public string RequestedSerialNumbersJson { get; set; } = "[]";

        // Navigation property
        public required StockTransfer StockTransfer { get; set; }
        public required ProductVariant ProductVariant { get; set; }
    }
}
