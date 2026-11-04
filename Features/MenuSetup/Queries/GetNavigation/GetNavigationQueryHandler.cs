using Inventory_Management_System.Database;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetNavigation
{
    // The menu tree of the logged-in user: only the pages their role may open, each with the
    // component codes of the actions they have on it.
    //
    //   Admin                      -> every page, every action.
    //   Page with menu actions     -> shown when the role has at least one of them.
    //   Page without menu actions  -> the page's fixed Permission decides, as before
    //                                 ("" = everyone, "admin" = admins only, else a permission).
    // Folders, submodules and modules with nothing visible inside are left out.
    public class GetNavigationQueryHandler(AppDbContext _db, ICurrentUser _currentUser, MenuActionAccess _access)
        : IRequestHandler<GetNavigationQuery, Result>
    {
        private const string AdminOnly = "admin";

        public async Task<Result> Handle(GetNavigationQuery request, CancellationToken cancellationToken)
        {
            const int active = (int)EntityStatus.Active;

            // Small queries, assembled in memory: menus are a few hundred rows at most.
            var modules = await _db.AppModules.AsNoTracking().Where(m => m.IsActive == active)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name).ToListAsync(cancellationToken);
            var submodules = await _db.AppSubmodules.AsNoTracking().Where(s => s.IsActive == active)
                .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken);
            var menus = await _db.AppMenus.AsNoTracking().Where(m => m.IsActive == active)
                .OrderBy(m => m.MenuOrder).ThenBy(m => m.Name).ToListAsync(cancellationToken);
            var actions = await _db.AppMenuActions.AsNoTracking().Where(a => a.IsActive == active)
                .Select(a => new { a.Id, a.MenuId, a.ComponentCode })
                .ToListAsync(cancellationToken);

            var isAdmin = _currentUser.IsAdmin;
            var granted = isAdmin
                ? new HashSet<int>()
                : (await _access.GetAsync(cancellationToken)).ActionsOf(_currentUser.RoleId);
            var actionsOfMenu = actions.ToLookup(a => a.MenuId);

            // Null = the user may not open this page.
            IReadOnlyList<string>? GrantedCodes(Entities.AppMenu menu)
            {
                var menuActions = actionsOfMenu[menu.Id].ToList();
                if (isAdmin) return menuActions.Select(a => a.ComponentCode).ToList();

                if (menuActions.Count > 0)
                {
                    var codes = menuActions.Where(a => granted.Contains(a.Id)).Select(a => a.ComponentCode).ToList();
                    return codes.Count > 0 ? codes : null;
                }

                // No actions configured for this page yet: its fixed permission decides.
                var permission = menu.Permission?.Trim() ?? string.Empty;
                if (permission.Length == 0) return [];
                if (string.Equals(permission, AdminOnly, StringComparison.OrdinalIgnoreCase)) return null;
                return _currentUser.HasPermission(permission) ? [] : null;
            }

            var childrenOf = menus.ToLookup(m => m.ParentId);

            IReadOnlyList<NavigationMenu> Build(int submoduleId, int? parentId)
            {
                var result = new List<NavigationMenu>();
                foreach (var m in childrenOf[parentId].Where(m => m.SubmoduleId == submoduleId))
                {
                    if (m.IsLeaf)
                    {
                        var codes = GrantedCodes(m);
                        if (codes is not null)
                            result.Add(new NavigationMenu(m.Id, m.Name, m.FrontendRoute, m.Icon, m.Permission, true, [], codes));
                    }
                    else
                    {
                        var children = Build(submoduleId, m.Id);
                        if (children.Count > 0)
                            result.Add(new NavigationMenu(m.Id, m.Name, m.FrontendRoute, m.Icon, m.Permission, false, children, []));
                    }
                }
                return result;
            }

            var tree = modules
                .Select(module => new NavigationModule(module.Id, module.Name, module.Icon,
                    submodules.Where(s => s.ModuleId == module.Id)
                        .Select(s => new NavigationSubmodule(s.Id, s.Name, Build(s.Id, null)))
                        .Where(s => s.Menus.Count > 0)
                        .ToList()))
                .Where(m => m.Submodules.Count > 0)
                .ToList();

            return MenuSetupResults.Ok(tree, "Navigation retrieved successfully");
        }
    }
}
