namespace Inventory_Management_System.Repositories.Interfaces
{
    public interface IAuthentication
    {
        Task<string>AuthenticateUser(string username, string password);
        public string GenerateJwtToken(string username, string password);
    }
}
