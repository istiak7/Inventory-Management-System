using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Shared
{
    // The rules every user command (create, update, delete) shares, so they cannot drift apart.
    //
    // Who may open Users & Roles is set in System Setup -> Menu Action, so it is not always an
    // admin. These rules make sure such a user cannot raise anyone's access (their own included)
    // above what an admin decided.
    public static class UserAccessRules
    {
        public static bool IsAdminRole(Role? role) =>
            role is not null && string.Equals(role.Name, MenuActionAccess.AdminRoleName, StringComparison.OrdinalIgnoreCase);

        // An active role (not deleted) by id, or null.
        public static Task<Role?> FindRoleAsync(AppDbContext db, int roleId, CancellationToken cancellationToken) =>
            db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.IsActive != (int)EntityStatus.Deleted, cancellationToken);

        // Only an admin may give the Admin role, or change or remove a user who has it.
        public static string? CheckAdminRoleChange(ICurrentUser currentUser, Role? currentRole, Role? newRole)
        {
            if (currentUser.IsAdmin) return null;
            if (IsAdminRole(newRole)) return "Only an admin can give a user the Admin role.";
            if (IsAdminRole(currentRole)) return "Only an admin can change or remove an admin user.";
            return null;
        }

        // Only the Admin role works across all branches; every other user belongs to one branch
        // (a user without a branch is never limited to one).
        public static async Task<string?> CheckBranchAsync(
            AppDbContext db, Role role, int? branchId, CancellationToken cancellationToken)
        {
            if (branchId is null)
                return IsAdminRole(role) ? null : "Choose a branch. Only the Admin role works across all branches.";

            var branchExists = await db.Branches.AnyAsync(b => b.Id == branchId.Value, cancellationToken);
            return branchExists ? null : "Selected branch does not exist";
        }

        // User names are unique without caring about upper/lower case ("Ali" and "ali" are the same).
        public static Task<bool> NameTakenAsync(
            AppDbContext db, string username, int? exceptUserId, CancellationToken cancellationToken)
        {
            var lower = username.ToLowerInvariant();
            return db.Users.AnyAsync(
                u => u.Name.ToLower() == lower && (exceptUserId == null || u.Id != exceptUserId),
                cancellationToken);
        }

        // True when `user` is the only active admin left: the system must always keep one, or
        // nobody could manage users again.
        public static async Task<bool> IsLastActiveAdminAsync(
            AppDbContext db, User user, Role userRole, CancellationToken cancellationToken)
        {
            if (!IsAdminRole(userRole) || user.IsActive != (int)EntityStatus.Active) return false;
            var others = await db.Users.CountAsync(u =>
                u.Id != user.Id && u.RoleId == user.RoleId && u.IsActive == (int)EntityStatus.Active,
                cancellationToken);
            return others == 0;
        }

        public const string LastAdminMessage = "This is the last active admin. Make another user admin first.";
    }
}
