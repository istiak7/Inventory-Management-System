using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Queries.GetModules
{
    public class GetModulesQueryHandler(AppDbContext _db) : IRequestHandler<GetModulesQuery, Result>
    {
        public async Task<Result> Handle(GetModulesQuery request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var modules = await _db.AppModules.AsNoTracking()
                .Where(m => m.IsActive != deleted)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
                .Select(m => new ModuleResponse(m.Id, m.Name, m.Icon, m.DisplayOrder,
                    m.Submodules.Count(s => s.IsActive != deleted)))
                .ToListAsync(cancellationToken);

            return MenuSetupResults.Ok(modules, "Modules retrieved successfully");
        }
    }
}
