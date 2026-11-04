using Inventory_Management_System.Database;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Shared.CurrentUser
{
    // Who may call which API route, read from System Setup -> Menu Action and the roles' grants.
    //
    // A route that is listed on a menu action is "managed": only roles that have one of the
    // actions listing it may call it. A route that no action lists is not managed, and the
    // endpoint's own fixed rule (RequirePermission / AdminOnly / logged in) still decides.
    //
    // The tables are small and read on every request, so they are kept in memory and reloaded
    // only after something changes (Invalidate is called by the commands that change them).
    public sealed class MenuActionAccess(IServiceScopeFactory _scopeFactory)
    {
        // The built-in administrative role: it always has every menu and action.
        public const string AdminRoleName = "Admin";

        // Routes that are part of simply being logged in. They are never managed by menu
        // actions, so a mistake in System Setup cannot lock everyone out of logging in or out,
        // seeing their own account or loading the menu.
        public static readonly IReadOnlySet<string> AlwaysAllowedRoutes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "POST /login",
                "POST /logout",
                "POST /refresh-token",
                "GET /me",
                "POST /change-password",
                "GET /get-navigation",
                "GET /health",
                // The setup warning and the page gates read it on every screen.
                "GET /get-system-setup-status",
            };

        private volatile Snapshot? _snapshot;
        private readonly SemaphoreSlim _loadLock = new(1, 1);

        // Call after changing menu actions, menus or a role's grants.
        public void Invalidate() => _snapshot = null;

        // "METHOD /route", the same text the Menu Action page stores (see ApiRouteCatalog).
        public static IEnumerable<string> RouteKeys(Endpoint? endpoint)
        {
            if (endpoint is not RouteEndpoint routeEndpoint) yield break;
            var pattern = "/" + (routeEndpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/');
            var methods = routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["ANY"];
            foreach (var method in methods) yield return $"{method} {pattern}";
        }

        public async Task<Snapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            var current = _snapshot;
            if (current is not null) return current;

            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                if (_snapshot is not null) return _snapshot;
                _snapshot = await LoadAsync(cancellationToken);
                return _snapshot;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private async Task<Snapshot> LoadAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            const int active = (int)EntityStatus.Active;

            // Only actions of menus that are still in use count.
            var actions = await db.AppMenuActions.AsNoTracking()
                .Where(a => a.IsActive == active && a.Menu.IsActive == active)
                .Select(a => new { a.Id, a.ApiRoutes })
                .ToListAsync(cancellationToken);

            var actionsByRoute = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in actions)
            {
                foreach (var route in action.ApiRoutes)
                {
                    var key = route.Trim();
                    if (key.Length == 0 || AlwaysAllowedRoutes.Contains(key)) continue;
                    if (!actionsByRoute.TryGetValue(key, out var ids))
                        actionsByRoute[key] = ids = [];
                    ids.Add(action.Id);
                }
            }

            var activeActionIds = actions.Select(a => a.Id).ToHashSet();
            var grants = await db.RoleMenuActions.AsNoTracking()
                .Where(g => g.IsActive == active && g.Role.IsActive == active)
                .Select(g => new { g.RoleId, g.MenuActionId })
                .ToListAsync(cancellationToken);

            var actionsByRole = grants
                .Where(g => activeActionIds.Contains(g.MenuActionId))
                .GroupBy(g => g.RoleId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.MenuActionId).ToHashSet());

            return new Snapshot(actionsByRoute, actionsByRole);
        }

        public sealed record Snapshot(
            IReadOnlyDictionary<string, HashSet<int>> ActionsByRoute,
            IReadOnlyDictionary<int, HashSet<int>> ActionsByRole)
        {
            public IReadOnlySet<int> ActionsOf(int? roleId) =>
                roleId is int id && ActionsByRole.TryGetValue(id, out var ids) ? ids : new HashSet<int>();
        }
    }
}
