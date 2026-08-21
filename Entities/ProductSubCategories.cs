namespace Inventory_Management_System.Entities
{
    public class ProductSubCategories : BaseEntity
    {
        public required string SubCategoryName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public required string Code { get; set; }
        public int ProductCategoryId { get; set; }

        // Navigation property
        public required ProductCategories ProductCategories { get; set; }
        public ICollection<Product> Products { get; set; } = [];

    }
}
