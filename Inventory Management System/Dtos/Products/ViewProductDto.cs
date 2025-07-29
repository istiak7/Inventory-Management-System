namespace Inventory_Management_System.Dtos.Products
{
    public class ViewProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public int Price { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
    }
}
