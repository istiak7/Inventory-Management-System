using Inventory_Management_System.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Dtos.SaleDto
{
    public class SaleManagerDto
    {
        public int WarehouseId { get;set; }
        public int SaleDetailsId { get; set; }
        public int ProductId { get; set; }
    }
}
