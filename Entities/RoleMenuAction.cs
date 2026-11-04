namespace Inventory_Management_System.Entities
{
    // Gives every user of a role one Menu Action (System Setup -> Menu Action).
    // A role can open a menu when it has at least one of that menu's actions, and can call an
    // API route when it has an action that lists that route.
    public class RoleMenuAction : BaseEntity
    {
        public int RoleId { get; set; }
        public int MenuActionId { get; set; }

        public Role Role { get; set; } = null!;
        public AppMenuAction MenuAction { get; set; } = null!;
    }
}
