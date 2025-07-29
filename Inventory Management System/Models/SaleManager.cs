using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Models
{
    public class SaleManager : BaseEntity
    {
        public int WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; }

        public int SaleDetailsId { get; set; }
        [ForeignKey(nameof(SaleDetailsId))]
        public virtual SaleDetails SaleDetails { get; set; }
    }
}
