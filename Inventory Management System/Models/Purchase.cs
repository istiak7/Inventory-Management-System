using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Models
{
    public class Purchase : BaseEntity
    {

        public int SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; }

        public int WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; }


    }
}
