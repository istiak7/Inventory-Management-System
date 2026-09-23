using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class ProductSerial : BaseEntity
    {
        public int ProductVariantId { get; set; } 
        public int SupplierPurchaseDetailsId { get; set; } 
        public int BranchId { get; set; } 
        public int? StockTransferDetailsId { get; set; }
        public required string SerialNumber { get; set; }   
        public SerialStatus Status { get; set; } = SerialStatus.InStock;
        public int WarrantyMonths { get; set; }          
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
        public DateTime? SoldDate { get; set; } = null;

        // Navigation property
        public required ProductVariant ProductVariant { get; set; }
        public required SupplierPurchaseDetails SupplierPurchaseDetails { get; set; }
        public required Branch Branch { get; set; }
        public StockTransferDetails? StockTransferDetails { get; set; }
        public SaleDetails? SaleDetails { get; set; } 
    }
}
