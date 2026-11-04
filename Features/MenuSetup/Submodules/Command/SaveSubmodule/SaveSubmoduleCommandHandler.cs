using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Submodules.Command.SaveSubmodule
{
    public class SaveSubmoduleCommandHandler(AppDbContext _db) : IRequestHandler<SaveSubmoduleCommand, Result>
    {
        public async Task<Result> Handle(SaveSubmoduleCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var name = request.Name.Trim();

            var module = await _db.AppModules.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.ModuleId && m.IsActive != deleted, cancellationToken);
            if (module is null) return MenuSetupResults.NotFound("Module");

            var duplicate = await _db.AppSubmodules.AnyAsync(s => s.IsActive != deleted && s.Id != request.Id
                && s.ModuleId == request.ModuleId && s.Name.ToLower() == name.ToLower(), cancellationToken);
            if (duplicate) return MenuSetupResults.Fail($"'{module.Name}' already has a submodule named '{name}'.");

            AppSubmodule? submodule;
            if (request.Id is int id)
            {
                submodule = await _db.AppSubmodules.FirstOrDefaultAsync(s => s.Id == id && s.IsActive != deleted, cancellationToken);
                if (submodule is null) return MenuSetupResults.NotFound("Submodule");
                submodule.UpDatedAt = DateTime.UtcNow;
            }
            else
            {
                submodule = new AppSubmodule { Name = name, CreatedAt = DateTime.UtcNow };
                _db.AppSubmodules.Add(submodule);
            }

            submodule.ModuleId = request.ModuleId;
            submodule.Name = name;
            submodule.DisplayOrder = request.DisplayOrder;
            await _db.SaveChangesAsync(cancellationToken);

            var response = new SubmoduleResponse(submodule.Id, module.Id, module.Name, submodule.Name, submodule.DisplayOrder,
                await _db.AppMenus.CountAsync(m => m.SubmoduleId == submodule.Id && m.IsActive != deleted, cancellationToken));
            return request.Id is null
                ? MenuSetupResults.Created(response, "Submodule created successfully")
                : MenuSetupResults.Ok(response, "Submodule updated successfully");
        }
    }
}
