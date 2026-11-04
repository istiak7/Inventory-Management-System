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
