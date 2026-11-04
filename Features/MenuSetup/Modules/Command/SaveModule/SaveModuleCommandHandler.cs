using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.Modules.Command.SaveModule
{
    public class SaveModuleCommandHandler(AppDbContext _db) : IRequestHandler<SaveModuleCommand, Result>
    {
        public async Task<Result> Handle(SaveModuleCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var name = request.Name.Trim();

            var duplicate = await _db.AppModules.AnyAsync(m => m.IsActive != deleted
                && m.Id != request.Id && m.Name.ToLower() == name.ToLower(), cancellationToken);
            if (duplicate) return MenuSetupResults.Fail($"A module named '{name}' already exists.");

            AppModule? module;
            if (request.Id is int id)
            {
                module = await _db.AppModules.FirstOrDefaultAsync(m => m.Id == id && m.IsActive != deleted, cancellationToken);
                if (module is null) return MenuSetupResults.NotFound("Module");
                module.UpDatedAt = DateTime.UtcNow;
            }
            else
            {
                module = new AppModule { Name = name, CreatedAt = DateTime.UtcNow };
                _db.AppModules.Add(module);
            }

            module.Name = name;
            module.Icon = request.Icon?.Trim() ?? string.Empty;
            module.DisplayOrder = request.DisplayOrder;
            await _db.SaveChangesAsync(cancellationToken);

            var response = new ModuleResponse(module.Id, module.Name, module.Icon, module.DisplayOrder,
                await _db.AppSubmodules.CountAsync(s => s.ModuleId == module.Id && s.IsActive != deleted, cancellationToken));
            return request.Id is null
                ? MenuSetupResults.Created(response, "Module created successfully")
                : MenuSetupResults.Ok(response, "Module updated successfully");
        }
    }
}
