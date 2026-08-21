using Inventory_Management_System.Entities.Common;

namespace Inventory_Management_System.Entities
{
    public class SupplierPurchaseDetails : BaseEntity
    {
        public int PurchaseId { get; set; } 
        public int ProductVariantId { get; set; } 
        public int OrderedQuantity { get; set; }       
        public int? ReceivedQuantity { get; set; }      
        public decimal UnitPrice { get; set; }        
        public decimal TotalAmount { get; set; }        
        public int WarrantyMonths { get; set; }        
        public LineStatus Status { get; set; } = LineStatus.Pending;

        // Navigation property
        public required SupplierPurchase SupplierPurchase { get; set; }
        public required ProductVariant ProductVariant { get; set; }
        public ICollection<ProductSerial> ProductSerials { get; set; } = [];
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
