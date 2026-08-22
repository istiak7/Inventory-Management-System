namespace Inventory_Management_System.Entities
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpireTime { get; set; }

        // The role decides the base set of permissions the user gets.
        public int RoleId { get; set; }

        // Null means "all branches" (used for admin). A value means the user
        // can only work inside that one branch (used for staff).
        public int? BranchId { get; set; }

        // Navigation properties
        public Role Role { get; set; }
        public Branch Branch { get; set; }

        // Extra permissions granted directly to this user, on top of the role.
        public ICollection<UserPermission> UserPermissions { get; set; } = [];
    }
}
