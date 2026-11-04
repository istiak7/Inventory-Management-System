using System.Security.Claims;

namespace Inventory_Management_System.Shared.CurrentUser
{
    // Reads the logged-in user from the JWT of the current request.
    public interface ICurrentUser
    {
        int? UserId { get; }
        string? Role { get; }
        int? RoleId { get; }
        bool IsAdmin { get; }

        // The fixed "permission" claims in the token (the fallback for routes and menus that no
        // menu action covers).
        bool HasPermission(string permission);

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

        public int? RoleId =>
            int.TryParse(Principal?.FindFirst("roleId")?.Value, out var roleId) ? roleId : null;

        public bool IsAdmin => string.Equals(Role, MenuActionAccess.AdminRoleName, StringComparison.OrdinalIgnoreCase);

        public bool HasPermission(string permission) =>
            Principal?.Claims.Any(c => c.Type == "permission" && c.Value == permission) ?? false;

        public int? BranchId =>
            int.TryParse(Principal?.FindFirst("branchId")?.Value, out var branchId) ? branchId : null;
    }
}
