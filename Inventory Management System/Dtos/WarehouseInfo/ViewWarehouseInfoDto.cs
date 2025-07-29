using System.Runtime.Serialization;

namespace Inventory_Management_System.Dtos.WarehouseInfo
{
    public class ViewWarehouseInfoDto
    {
        public int PurchaseId { get; set; }
        public string Supplier { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}
