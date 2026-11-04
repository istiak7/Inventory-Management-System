using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetNavigation
{
    public class GetNavigationQueryHandler(AppDbContext _db) : IRequestHandler<GetNavigationQuery, Result>
    {
        public async Task<Result> Handle(GetNavigationQuery request, CancellationToken cancellationToken)
        {
            const int active = (int)EntityStatus.Active;

            // Three small queries, assembled in memory: menus are a few hundred rows at most.
            var modules = await _db.AppModules.AsNoTracking().Where(m => m.IsActive == active)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name).ToListAsync(cancellationToken);
            var submodules = await _db.AppSubmodules.AsNoTracking().Where(s => s.IsActive == active)
                .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken);
            var menus = await _db.AppMenus.AsNoTracking().Where(m => m.IsActive == active)
                .OrderBy(m => m.MenuOrder).ThenBy(m => m.Name).ToListAsync(cancellationToken);

            var childrenOf = menus.ToLookup(m => m.ParentId);

            IReadOnlyList<NavigationMenu> Build(int submoduleId, int? parentId) =>
                childrenOf[parentId]
                    .Where(m => m.SubmoduleId == submoduleId)
                    .Select(m => new NavigationMenu(m.Id, m.Name, m.FrontendRoute, m.Icon, m.Permission, m.IsLeaf,
                        m.IsLeaf ? [] : Build(submoduleId, m.Id)))
                    .ToList();

            var tree = modules.Select(module => new NavigationModule(module.Id, module.Name, module.Icon,
                    submodules.Where(s => s.ModuleId == module.Id)
                        .Select(s => new NavigationSubmodule(s.Id, s.Name, Build(s.Id, null)))
                        .ToList()))
                .ToList();

            return MenuSetupResults.Ok(tree, "Navigation retrieved successfully");
        }
    }
}
