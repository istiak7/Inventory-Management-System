using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.MenuSetup.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.MenuSetup.MenuActions.Command.DeleteMenuAction
{
    public class DeleteMenuActionCommandHandler(AppDbContext _db) : IRequestHandler<DeleteMenuActionCommand, Result>
    {
        public async Task<Result> Handle(DeleteMenuActionCommand request, CancellationToken cancellationToken)
        {
            var action = await _db.AppMenuActions
                .FirstOrDefaultAsync(a => a.Id == request.Id && a.IsActive != (int)EntityStatus.Deleted, cancellationToken);
            if (action is null) return MenuSetupResults.NotFound("Menu action");

            action.Delete();
            action.UpDatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return MenuSetupResults.Ok(action.Id, "Menu action deleted successfully");
        }
    }
}
