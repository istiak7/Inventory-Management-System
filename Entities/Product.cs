namespace Inventory_Management_System.Entities
{
    public class Product : BaseEntity
    {
        public int ProductSubCategoryId { get; set; }
        public int BrandId { get; set; }
        public required string ProductName { get; set; }
        public string ProductDescription { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        // False = this product can never be taken back (sales return) or sent back (purchase return).
        public bool IsReturnable { get; set; } = true;

        // Navigation property
        public required ProductSubCategories ProductSubCategories { get; set; }
        public required Brand Brand { get; set; }
        public ICollection<ProductVariant> ProductVariants { get; set; } = [];
    }
}
