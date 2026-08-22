using System.Security.Claims;

namespace Inventory_Management_System.Shared.CurrentUser
{
    // Reads the logged-in user from the JWT of the current request.
    public interface ICurrentUser
    {
        int? UserId { get; }
        string? Role { get; }
        bool IsAdmin { get; }

        // Null = the user can access all branches (admin or system).
        int? BranchId { get; }

        bool IsAuthenticated { get; }
    }

    public class CurrentUserService(IHttpContextAccessor _accessor) : ICurrentUser
    {
        private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

        public int? UserId =>
            int.TryParse(Principal?.FindFirst("id")?.Value, out var id) ? id : null;

        public string? Role => Principal?.FindFirst(ClaimTypes.Role)?.Value;

        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

        public int? BranchId =>
            int.TryParse(Principal?.FindFirst("branchId")?.Value, out var branchId) ? branchId : null;
    }
}
