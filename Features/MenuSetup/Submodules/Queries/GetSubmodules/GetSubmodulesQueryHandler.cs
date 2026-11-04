using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Queries.GetSubmodules
{
    public class GetSubmodulesQueryHandler(AppDbContext _db) : IRequestHandler<GetSubmodulesQuery, Result>
    {
        public async Task<Result> Handle(GetSubmodulesQuery request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var submodules = await _db.AppSubmodules.AsNoTracking()
                .Where(s => s.IsActive != deleted && s.Module.IsActive != deleted)
                .OrderBy(s => s.Module.DisplayOrder).ThenBy(s => s.DisplayOrder).ThenBy(s => s.Name)
                .Select(s => new SubmoduleResponse(s.Id, s.ModuleId, s.Module.Name, s.Name, s.DisplayOrder,
                    s.Menus.Count(m => m.IsActive != deleted)))
                .ToListAsync(cancellationToken);

            return MenuSetupResults.Ok(submodules, "Submodules retrieved successfully");
        }
    }
}
