namespace Inventory_Management_System.Entities
{
    public class Product : BaseEntity
    {
        public int ProductSubCategoryId { get; set; } //FK
        public int BrandId { get; set; } //FK
        public required string ProductName { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public required string ProductCode { get; set; }
        public decimal ProductPrice { get; set; }

        public required ProductSubCategories ProductSubCategories { get; set; }
        public required Brand Brand { get; set; }
    }
}
