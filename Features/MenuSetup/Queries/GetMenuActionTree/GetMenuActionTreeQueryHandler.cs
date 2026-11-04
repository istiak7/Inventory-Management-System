using Inventory_Management_System.Database;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Queries.GetMenuActionTree
{
    public class GetMenuActionTreeQueryHandler(AppDbContext _db) : IRequestHandler<GetMenuActionTreeQuery, Result>
    {
        public async Task<Result> Handle(GetMenuActionTreeQuery request, CancellationToken cancellationToken)
        {
            const int active = (int)EntityStatus.Active;

            var modules = await _db.AppModules.AsNoTracking().Where(m => m.IsActive == active)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name).ToListAsync(cancellationToken);
            var submodules = await _db.AppSubmodules.AsNoTracking().Where(s => s.IsActive == active)
                .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name).ToListAsync(cancellationToken);
            var menus = await _db.AppMenus.AsNoTracking().Where(m => m.IsActive == active)
                .OrderBy(m => m.MenuOrder).ThenBy(m => m.Name).ToListAsync(cancellationToken);
            var actions = await _db.AppMenuActions.AsNoTracking().Where(a => a.IsActive == active)
                .OrderBy(a => a.Id)
                .Select(a => new { a.Id, a.MenuId, a.Name, a.ComponentCode })
                .ToListAsync(cancellationToken);

            var actionsOf = actions.ToLookup(a => a.MenuId);
            var childrenOf = menus.ToLookup(m => m.ParentId);

            // Pages in menu order (depth first), each with the folders above it.
            IEnumerable<MenuActionTreePage> Pages(int submoduleId, int? parentId, List<string> path)
            {
                foreach (var m in childrenOf[parentId].Where(m => m.SubmoduleId == submoduleId))
                {
                    if (m.IsLeaf)
                    {
                        yield return new MenuActionTreePage(m.Id, m.Name, m.FrontendRoute, path.ToList(),
                            actionsOf[m.Id].Select(a => new MenuActionTreeAction(a.Id, a.Name, a.ComponentCode)).ToList());
                    }
                    else
                    {
                        foreach (var page in Pages(submoduleId, m.Id, [.. path, m.Name])) yield return page;
                    }
                }
            }

            var tree = modules
                .Select(module => new MenuActionTreeModule(module.Id, module.Name,
                    submodules.Where(s => s.ModuleId == module.Id)
                        .SelectMany(s => Pages(s.Id, null, []))
                        .ToList()))
                .Where(m => m.Pages.Count > 0)
                .ToList();

            return MenuSetupResults.Ok(tree, "Menu action tree retrieved successfully");
        }
    }
}
