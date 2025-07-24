using Inventory_Management_System.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_Management_System.Dtos.Purchase
{
    public class CreatePurchaseRequestDto
    {
        public int SupplierID { get; set; }
        public int WarehouseID { get; set; }
        public List<PurchaseProductDto> Products { get; set; }

    }
}
