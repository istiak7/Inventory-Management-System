namespace Inventory_Management_System.Entities
{
    public class Role : BaseEntity
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;

    }
}
