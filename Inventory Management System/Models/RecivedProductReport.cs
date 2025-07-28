using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Models
{
    public class RecivedProductReport : BaseEntity
    {
        public int StockId { get; set; }
        [ForeignKey(nameof(StockId))]
        public virtual Stock Stock { get; set; }

        public int Normal { get; set; }
        public int Damage { get; set; }
        public int Missing { get; set; }
    }
}
