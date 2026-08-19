namespace Inventory_Management_System.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }

    }

    public class RolePermission : BaseEntity
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
    }
}
