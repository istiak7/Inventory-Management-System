namespace Inventory_Management_System.Features.Users.Login
{
    public class LoginResponse
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; }

        // Null = the user can access all branches (admin).
        public int? BranchId { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
