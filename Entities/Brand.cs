namespace Inventory_Management_System.Entities
{
    public class Brand : BaseEntity
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Product> Products { get; set; } = [];
    }
}
