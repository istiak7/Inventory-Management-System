using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Menus.Queries.GetMenus
{
    public class GetMenusQueryHandler(AppDbContext _db) : IRequestHandler<GetMenusQuery, Result>
    {
        public async Task<Result> Handle(GetMenusQuery request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var menus = await _db.AppMenus.AsNoTracking()
                .Where(m => m.SubmoduleId == request.SubmoduleId && m.IsActive != deleted)
                .OrderBy(m => m.MenuOrder).ThenBy(m => m.Name)
                .Select(m => new MenuResponse(m.Id, m.SubmoduleId, m.ParentId, m.Name, m.FrontendRoute, m.MenuOrder,
                    m.IsLeaf, m.Icon, m.Permission,
                    m.Children.Count(c => c.IsActive != deleted),
                    m.Actions.Count(a => a.IsActive != deleted)))
                .ToListAsync(cancellationToken);

            return MenuSetupResults.Ok(menus, "Menus retrieved successfully");
        }
    }
}
