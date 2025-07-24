using Inventory_Management_System.Dtos.Purchase;

namespace Inventory_Management_System.Dtos.Sale
{
    public class CreateSaleRequestDto
    {
        public int CustomerID { get; set; }
        public int WarehouseID { get; set; }
        public List<PurchaseProductDto> Products { get; set; }
    }
}
