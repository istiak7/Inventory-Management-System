using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    // One physical serialized unit (laptop, GPU, ...). Bound to the lot it arrived in
    // (SupplierPurchaseDetails), so its purchase cost is always lot.UnitPrice and its warranty
    // is copied from the lot at receipt — never recomputed or duplicated as a separate cost field.
    public class ProductSerial : BaseEntity
    {
        public int ProductVariantId { get; set; } //FK
        public int SupplierPurchaseDetailsId { get; set; } //FK -> the lot (cost + supplier lineage)
        public int BranchId { get; set; } //FK
        public required string SerialNumber { get; set; }    // globally unique across the whole table (unique index)
        public SerialStatus Status { get; set; } = SerialStatus.InStock;
        public int WarrantyMonths { get; set; }              // copied from the lot at receipt
        public DateTime ReceivedDate { get; set; } = DateTime.Now;
        public DateTime? SoldDate { get; set; } = null;

        // Navigation property
        public required ProductVariant ProductVariant { get; set; }
        public required SupplierPurchaseDetails SupplierPurchaseDetails { get; set; }
        public required Branch Branch { get; set; }
        public SaleDetails? SaleDetails { get; set; } // null if not yet sold
    }
}
