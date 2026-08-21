namespace Inventory_Management_System.Entities
{
    public class ProductCategories : BaseEntity
    {
        public required string CategoryName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

        // Navigation property
        public ICollection<ProductSubCategories> ProductSubCategories { get; set; } = [];

    }
}
