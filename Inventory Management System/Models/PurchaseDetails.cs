using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Models
{
    public class PurchaseDetails : BaseEntity
    {
        public int PurchaseId { get; set; }
        [ForeignKey(nameof(PurchaseId))]
        public virtual Purchase Purchase { get; set; }

        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; }

        public int Quantity { get; set; }
        public int Price { get; set; }
    }
}
