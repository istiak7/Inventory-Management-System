namespace Inventory_Management_System.Models
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ? Address { get; set; }
    }
}
