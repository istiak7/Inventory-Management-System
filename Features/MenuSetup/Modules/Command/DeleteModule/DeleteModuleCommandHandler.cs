using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Command.DeleteModule
{
    public class DeleteModuleCommandHandler(AppDbContext _db) : IRequestHandler<DeleteModuleCommand, Result>
    {
        public async Task<Result> Handle(DeleteModuleCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var module = await _db.AppModules.FirstOrDefaultAsync(m => m.Id == request.Id && m.IsActive != deleted, cancellationToken);
            if (module is null) return MenuSetupResults.NotFound("Module");

            if (await _db.AppSubmodules.AnyAsync(s => s.ModuleId == module.Id && s.IsActive != deleted, cancellationToken))
                return MenuSetupResults.Fail($"'{module.Name}' still has submodules. Delete or move them first.");

            module.Delete();
            module.UpDatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return MenuSetupResults.Ok(module.Id, "Module deleted successfully");
        }
    }
}
