using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Roles.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Roles.Command.DeleteRole
{
    // Removes a role that nobody uses. Admin can never be removed, and a role that still has
    // users must first have them moved to another role.
    public class DeleteRoleCommandHandler(
        AppDbContext _db,
        MenuActionAccess _access,
        ILogger<DeleteRoleCommandHandler> _logger
    ) : IRequestHandler<DeleteRoleCommand, Result>
    {
        public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var role = await _db.Roles
                .FirstOrDefaultAsync(r => r.Id == request.Id && r.IsActive != deleted, cancellationToken);
            if (role is null) return Fail(404, "Role not found");

            if (RoleMenuActionGrants.IsBuiltInAdmin(role))
                return Fail(400, "The Admin role is built in and cannot be deleted");

            var activeUsers = await _db.Users
                .CountAsync(u => u.RoleId == role.Id && u.IsActive != deleted, cancellationToken);
            if (activeUsers > 0)
                return Fail(400, $"{activeUsers} user(s) still have this role. Give them another role first.");

            try
            {
                // Removed users still point at the role, so then it is only marked deleted;
                // a role nobody ever had is removed for good (its grants go with it).
                var everUsed = await _db.Users.AnyAsync(u => u.RoleId == role.Id, cancellationToken);
                if (everUsed)
                {
                    role.Delete();
                    role.UpDatedAt = DateTime.UtcNow;
                    var grants = await _db.RoleMenuActions.Where(g => g.RoleId == role.Id).ToListAsync(cancellationToken);
                    _db.RoleMenuActions.RemoveRange(grants);
                }
                else
                {
                    _db.Roles.Remove(role);
                }

                await _db.SaveChangesAsync(cancellationToken);
                _access.Invalidate();

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Role deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role {RoleId}", request.Id);
                return Fail(500, "An error occurred while deleting the role.");
            }
        }

        private static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message
        };
    }
}
