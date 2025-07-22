namespace Inventory_Management_System.Models
{
    public class Stock
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public int CurrentStock { get; set; }
    }
}
