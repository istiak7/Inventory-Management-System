using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class SaleDetails : BaseEntity
    {
        public int SaleId { get; set; } 
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }           
        // What this unit cost us, snapshotted when the sale is made so gross profit stays fixed
        // even if the product is later bought in at a different price. Serialized units carry the
        // exact cost of the lot they came from; everything else uses the weighted average.
        public decimal UnitCost { get; set; }
        public decimal? DiscountPerItem { get; set; }    
        public decimal TotalAmount { get; set; }         
        public int? WarrantyMonths { get; set; }         
        public int? ProductSerialId { get; set; } 
        public SaleLineStatus Status { get; set; } = SaleLineStatus.Completed;

        // Navigation property
        public required CustomerSale CustomerSale { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public ProductSerial? ProductSerial { get; set; }

        // Cost of goods sold for this line. Discounts reduce revenue, never cost.
        public decimal CostOfGoodsSold => UnitCost * Quantity;
    }
}
