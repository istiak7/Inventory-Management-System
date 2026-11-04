namespace Inventory_Management_System.Entities
{
    public class Role : BaseEntity
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;

        // Permissions this role grants.
        public ICollection<RolePermission> RolePermissions { get; set; } = [];

        // Menu actions this role grants (System Setup -> Menu Action). This is what decides
        // which menus the role sees and which API routes it may call.
        public ICollection<RoleMenuAction> RoleMenuActions { get; set; } = [];
    }
}
