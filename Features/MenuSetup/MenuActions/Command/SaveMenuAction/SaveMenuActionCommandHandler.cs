using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Command.SaveMenuAction
{
    public class SaveMenuActionCommandHandler(AppDbContext _db, EndpointDataSource _endpoints)
        : IRequestHandler<SaveMenuActionCommand, Result>
    {
        public async Task<Result> Handle(SaveMenuActionCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var name = request.Name.Trim();
            var code = request.ComponentCode.Trim();

            var menu = await _db.AppMenus.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.MenuId && m.IsActive != deleted, cancellationToken);
            if (menu is null) return MenuSetupResults.NotFound("Menu");
            if (!menu.IsLeaf) return MenuSetupResults.Fail($"'{menu.Name}' is a folder. Actions belong to pages.");

            // Only routes the API really has: a typo would grant nothing and nobody would notice.
            var known = ApiRouteCatalog.List(_endpoints).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var routes = request.ApiRoutes.Select(r => r.Trim()).Where(r => r.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var unknown = routes.Where(r => !known.Contains(r)).ToList();
            if (unknown.Count > 0) return MenuSetupResults.Fail($"Unknown API route(s): {string.Join(", ", unknown)}.");

            var siblings = _db.AppMenuActions.Where(a => a.MenuId == request.MenuId && a.IsActive != deleted && a.Id != request.Id);
            if (await siblings.AnyAsync(a => a.Name.ToLower() == name.ToLower(), cancellationToken))
                return MenuSetupResults.Fail($"'{menu.Name}' already has an action named '{name}'.");
            if (await siblings.AnyAsync(a => a.ComponentCode == code, cancellationToken))
                return MenuSetupResults.Fail($"'{menu.Name}' already uses component code '{code}'.");

            AppMenuAction? action;
            if (request.Id is int id)
            {
                action = await _db.AppMenuActions.FirstOrDefaultAsync(a => a.Id == id && a.IsActive != deleted, cancellationToken);
                if (action is null) return MenuSetupResults.NotFound("Menu action");
                action.UpDatedAt = DateTime.UtcNow;
            }
            else
            {
                action = new AppMenuAction { Name = name, ComponentCode = code, CreatedAt = DateTime.UtcNow };
                _db.AppMenuActions.Add(action);
            }

            action.MenuId = request.MenuId;
            action.Name = name;
            action.ComponentCode = code;
            action.ApiRoutes = routes;
            await _db.SaveChangesAsync(cancellationToken);

            var response = new MenuActionResponse(action.Id, action.MenuId, action.Name, action.ComponentCode, action.ApiRoutes);
            return request.Id is null
                ? MenuSetupResults.Created(response, "Menu action created successfully")
                : MenuSetupResults.Ok(response, "Menu action updated successfully");
        }
    }
}
