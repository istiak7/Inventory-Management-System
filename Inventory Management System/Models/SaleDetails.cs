using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Models
{
    public class SaleDetails : BaseEntity
    {
        public int SaleId { get; set; }
        [ForeignKey(nameof(SaleId))]
        public virtual Sale Sale { get;set; }

        public int WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; }

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
    }
}
