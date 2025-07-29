using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Models
{
    public class Stock : BaseEntity
    {
        public int WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; }

        public int PurchaseDetailsId { get; set; }
        [ForeignKey(nameof(PurchaseDetailsId))]
        public virtual PurchaseDetails PurchaseDetails { get; set; }

        public int Quantity { get; set; }

        public string Status { get; set; } = "Pending";
    }
}
