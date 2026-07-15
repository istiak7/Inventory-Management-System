namespace Inventory_Management_System.Shared
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = null!;
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public int ExpiryMinutes { get; set; }
    }
}
