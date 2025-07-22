namespace Inventory_Management_System.Models
{
    public class Purchase
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public int Quantity { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
