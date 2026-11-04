using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Roles.Shared
{
    // Saves which menu actions (System Setup -> Menu Action) a role has.
    public static class RoleMenuActionGrants
    {
        public static bool IsBuiltInAdmin(Role role) =>
            string.Equals(role.Name, MenuActionAccess.AdminRoleName, StringComparison.OrdinalIgnoreCase);

        // Keeps only ids of menu actions that exist and are active, without duplicates.
        public static async Task<List<int>> ValidIdsAsync(
            AppDbContext db, IEnumerable<int> ids, CancellationToken cancellationToken)
        {
            var wanted = ids.Distinct().ToList();
            if (wanted.Count == 0) return wanted;
            return await db.AppMenuActions
                .Where(a => wanted.Contains(a.Id) && a.IsActive == (int)EntityStatus.Active)
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);
        }

        // Who may open Users & Roles is set in System Setup, so it is not always an admin. A
        // non-admin may not change the Admin role or their own role, and may only hand out menu
        // actions their own role already has — otherwise they could give themselves more access.
        // `role` is null for a new role. Returns the reason when refused.
        public static async Task<string?> CheckEditorAsync(
            AppDbContext db,
            ICurrentUser currentUser,
            MenuActionAccess access,
            Role? role,
            IEnumerable<int>? menuActionIds,
            CancellationToken cancellationToken)
        {
            if (currentUser.IsAdmin) return null;
            if (role is not null && IsBuiltInAdmin(role)) return "Only an admin can change the Admin role.";
            if (role is not null && role.Id == currentUser.RoleId) return "You cannot change your own role. Ask an admin.";
            if (menuActionIds is null) return null;

            var wanted = await ValidIdsAsync(db, menuActionIds, cancellationToken);
            var current = role is null
                ? []
                : (await db.RoleMenuActions.Where(g => g.RoleId == role.Id).Select(g => g.MenuActionId)
                    .ToListAsync(cancellationToken)).ToHashSet();
            var own = (await access.GetAsync(cancellationToken)).ActionsOf(currentUser.RoleId);

            return wanted.Any(id => !current.Contains(id) && !own.Contains(id))
                ? "You can only give a role menu actions that your own role has."
                : null;
        }

        // Makes the role's grants exactly `menuActionIds` (adds the new ones, removes the rest).
        // The caller saves the changes.
        public static async Task ReplaceAsync(
            AppDbContext db, Role role, IEnumerable<int> menuActionIds, CancellationToken cancellationToken)
        {
            var wanted = (await ValidIdsAsync(db, menuActionIds, cancellationToken)).ToHashSet();
            var current = await db.RoleMenuActions.Where(g => g.RoleId == role.Id).ToListAsync(cancellationToken);

            db.RoleMenuActions.RemoveRange(current.Where(g => !wanted.Contains(g.MenuActionId)));
            var have = current.Select(g => g.MenuActionId).ToHashSet();
            foreach (var id in wanted.Where(id => !have.Contains(id)))
                db.RoleMenuActions.Add(new RoleMenuAction { Role = role, MenuActionId = id });
        }
    }
}
