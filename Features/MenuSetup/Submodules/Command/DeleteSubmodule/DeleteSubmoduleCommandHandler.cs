using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Command.DeleteSubmodule
{
    public class DeleteSubmoduleCommandHandler(AppDbContext _db) : IRequestHandler<DeleteSubmoduleCommand, Result>
    {
        public async Task<Result> Handle(DeleteSubmoduleCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var submodule = await _db.AppSubmodules.FirstOrDefaultAsync(s => s.Id == request.Id && s.IsActive != deleted, cancellationToken);
            if (submodule is null) return MenuSetupResults.NotFound("Submodule");

            if (await _db.AppMenus.AnyAsync(m => m.SubmoduleId == submodule.Id && m.IsActive != deleted, cancellationToken))
                return MenuSetupResults.Fail($"'{submodule.Name}' still has menus. Delete or move them first.");

            submodule.Delete();
            submodule.UpDatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return MenuSetupResults.Ok(submodule.Id, "Submodule deleted successfully");
        }
    }
}
