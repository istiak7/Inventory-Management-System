namespace Inventory_Management_System.Entities
{
    public class User : BaseEntity
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpireTime { get; set; }
        public int RoleId { get; set; }


        // Navigation property to Role
        public ICollection<Role> Role { get; set; }
        public ICollection<Permission> Permission { get; set; }
    }
}
