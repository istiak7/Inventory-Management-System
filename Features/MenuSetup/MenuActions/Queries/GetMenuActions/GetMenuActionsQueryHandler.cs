using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Queries.GetMenuActions
{
    public class GetMenuActionsQueryHandler(AppDbContext _db) : IRequestHandler<GetMenuActionsQuery, Result>
    {
        public async Task<Result> Handle(GetMenuActionsQuery request, CancellationToken cancellationToken)
        {
            var actions = await _db.AppMenuActions.AsNoTracking()
                .Where(a => a.MenuId == request.MenuId && a.IsActive != (int)EntityStatus.Deleted)
                .OrderBy(a => a.ComponentCode).ThenBy(a => a.Name)
                .Select(a => new MenuActionResponse(a.Id, a.MenuId, a.Name, a.ComponentCode, a.ApiRoutes))
                .ToListAsync(cancellationToken);

            return MenuSetupResults.Ok(actions, "Menu actions retrieved successfully");
        }
    }
}
