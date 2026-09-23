namespace Inventory_Management_System.Entities
{
    /// <summary>
    /// One "stay signed in" token per device. A user signed in on two computers has two rows,
    /// so signing in on one never signs the other out. Only a hash of the token is stored.
    /// </summary>
    public class UserRefreshToken : BaseEntity
    {
        public int UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        public User User { get; set; } = null!;
    }
}
