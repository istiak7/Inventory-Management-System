using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Command.SaveMenu
{
    public class SaveMenuCommandHandler(AppDbContext _db) : IRequestHandler<SaveMenuCommand, Result>
    {
        public async Task<Result> Handle(SaveMenuCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var name = request.Name.Trim();
            var route = request.IsLeaf ? request.FrontendRoute!.Trim() : string.Empty;

            if (!await _db.AppSubmodules.AnyAsync(s => s.Id == request.SubmoduleId && s.IsActive != deleted, cancellationToken))
                return MenuSetupResults.NotFound("Submodule");

            if (request.ParentId is int parentId)
            {
                if (parentId == request.Id) return MenuSetupResults.Fail("A menu cannot be moved into itself.");

                var parent = await _db.AppMenus.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == parentId && m.IsActive != deleted, cancellationToken);
                if (parent is null) return MenuSetupResults.NotFound("Parent menu");
                if (parent.SubmoduleId != request.SubmoduleId)
                    return MenuSetupResults.Fail("The parent menu belongs to another submodule.");
                if (parent.IsLeaf)
                    return MenuSetupResults.Fail($"'{parent.Name}' is a page, not a folder. Only folders can hold menus.");

                // Moving a folder under one of its own sub-menus would make a loop.
                if (request.Id is int selfId && await IsInsideAsync(parentId, selfId, cancellationToken))
                    return MenuSetupResults.Fail("A menu cannot be moved under one of its own sub-menus.");
            }

            var duplicateName = await _db.AppMenus.AnyAsync(m => m.IsActive != deleted && m.Id != request.Id
                && m.SubmoduleId == request.SubmoduleId && m.ParentId == request.ParentId
                && m.Name.ToLower() == name.ToLower(), cancellationToken);
            if (duplicateName) return MenuSetupResults.Fail($"A menu named '{name}' already exists at this level.");

            if (route.Length > 0 && await _db.AppMenus.AnyAsync(m => m.IsActive != deleted && m.Id != request.Id
                    && m.IsLeaf && m.FrontendRoute == route, cancellationToken))
                return MenuSetupResults.Fail($"Another menu already opens '{route}'.");

            AppMenu? menu;
            if (request.Id is int id)
            {
                menu = await _db.AppMenus.FirstOrDefaultAsync(m => m.Id == id && m.IsActive != deleted, cancellationToken);
                if (menu is null) return MenuSetupResults.NotFound("Menu");

                if (request.IsLeaf && await _db.AppMenus.AnyAsync(m => m.ParentId == id && m.IsActive != deleted, cancellationToken))
                    return MenuSetupResults.Fail("This menu has sub-menus, so it must stay a folder.");
                if (menu.SubmoduleId != request.SubmoduleId && await _db.AppMenus.AnyAsync(m => m.ParentId == id && m.IsActive != deleted, cancellationToken))
                    return MenuSetupResults.Fail("Move or delete the sub-menus before moving this folder to another submodule.");

                menu.UpDatedAt = DateTime.UtcNow;
            }
            else
            {
                menu = new AppMenu { Name = name, CreatedAt = DateTime.UtcNow };
                _db.AppMenus.Add(menu);
            }

            menu.SubmoduleId = request.SubmoduleId;
            menu.ParentId = request.ParentId;
            menu.Name = name;
            menu.FrontendRoute = route;
            menu.MenuOrder = request.MenuOrder;
            menu.IsLeaf = request.IsLeaf;
            menu.Icon = request.Icon?.Trim() ?? string.Empty;
            menu.Permission = request.Permission?.Trim() ?? string.Empty;
            await _db.SaveChangesAsync(cancellationToken);

            var response = new MenuResponse(menu.Id, menu.SubmoduleId, menu.ParentId, menu.Name, menu.FrontendRoute,
                menu.MenuOrder, menu.IsLeaf, menu.Icon, menu.Permission,
                await _db.AppMenus.CountAsync(m => m.ParentId == menu.Id && m.IsActive != deleted, cancellationToken),
                await _db.AppMenuActions.CountAsync(a => a.MenuId == menu.Id && a.IsActive != deleted, cancellationToken));
            return request.Id is null
                ? MenuSetupResults.Created(response, "Menu created successfully")
                : MenuSetupResults.Ok(response, "Menu updated successfully");
        }

        // True when `menuId` is `ancestorId` itself or sits somewhere below it.
        private async Task<bool> IsInsideAsync(int menuId, int ancestorId, CancellationToken cancellationToken)
        {
            var parents = await _db.AppMenus.AsNoTracking()
                .Where(m => m.IsActive != (int)EntityStatus.Deleted)
                .Select(m => new { m.Id, m.ParentId })
                .ToDictionaryAsync(m => m.Id, m => m.ParentId, cancellationToken);

            int? current = menuId;
            var seen = new HashSet<int>();
            while (current is int c && seen.Add(c))
            {
                if (c == ancestorId) return true;
                current = parents.GetValueOrDefault(c);
            }
            return false;
        }
    }
}
