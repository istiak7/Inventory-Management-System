namespace Inventory_Management_System.Entities
{
    public class Stock : BaseEntity
    {
        public int BranchId { get; set; } //FK
        public int ProductVariantId { get; set; }  //FK
        public int CurrentStock { get; set; }

        // Navigation property
        public required Branch Branch { get; set; }
        public required ProductVariant ProductVariant { get; set; }
    }
}
