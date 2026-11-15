namespace Inventory_Management_System.Entities
{
    // A measurement unit a product is counted in, e.g. Piece (pcs), Kilogram (kg), Meter (m).
    // Status uses BaseEntity.IsActive: Active units can be picked for new products,
    // InActive units stay on the products that already use them.
    public class UnitOfMeasure : BaseEntity
    {
        public required string Name { get; set; }
        public required string ShortName { get; set; }
        public string Description { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Product> Products { get; set; } = [];
    }
}
