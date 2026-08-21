namespace Inventory_Management_System.Entities
{
    public class StockTransferDetails : BaseEntity
    {
        public int StockTransferId { get; set; }  
        public int ProductVariantId { get; set; }  
        public int Quantity { get; set; }
        public string RequestedSerialNumbersJson { get; set; } = "[]";

        // Navigation property
        public required StockTransfer StockTransfer { get; set; }
        public required ProductVariant ProductVariant { get; set; }
    }
}
