using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Command.DeleteMenu
{
    public class DeleteMenuCommandHandler(AppDbContext _db) : IRequestHandler<DeleteMenuCommand, Result>
    {
        public async Task<Result> Handle(DeleteMenuCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var menu = await _db.AppMenus.FirstOrDefaultAsync(m => m.Id == request.Id && m.IsActive != deleted, cancellationToken);
            if (menu is null) return MenuSetupResults.NotFound("Menu");

            if (await _db.AppMenus.AnyAsync(m => m.ParentId == menu.Id && m.IsActive != deleted, cancellationToken))
                return MenuSetupResults.Fail($"'{menu.Name}' still has sub-menus. Delete or move them first.");

            // A page's actions go with it.
            var actions = await _db.AppMenuActions.Where(a => a.MenuId == menu.Id && a.IsActive != deleted).ToListAsync(cancellationToken);
            foreach (var action in actions) action.Delete();

            menu.Delete();
            menu.UpDatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return MenuSetupResults.Ok(menu.Id, "Menu deleted successfully");
        }
    }
}
