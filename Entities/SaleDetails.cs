using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class SaleDetails : BaseEntity
    {
        public int SaleId { get; set; } 
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }           
        public decimal? DiscountPerItem { get; set; }    
        public decimal TotalAmount { get; set; }         
        public int? WarrantyMonths { get; set; }         
        public int? ProductSerialId { get; set; } 
        public SaleLineStatus Status { get; set; } = SaleLineStatus.Completed;

        // Navigation property
        public required CustomerSale CustomerSale { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public ProductSerial? ProductSerial { get; set; }
    }
}
