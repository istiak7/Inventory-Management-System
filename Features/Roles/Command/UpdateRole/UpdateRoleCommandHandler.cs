using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Roles.Command.UpdateRole
{
    public class UpdateRoleCommandHandler(
        AppDbContext _db,
        ILogger<UpdateRoleCommandHandler> _logger
    ) : IRequestHandler<UpdateRoleCommand, Result>
    {
        private const string AdminRoleName = "Admin";

        public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

            if (role is null)
            {
                return Fail(404, "Role not found");
            }

            // Admin rights are tied to the role called "Admin", so renaming it would lock every
            // admin out at once.
            if (string.Equals(role.Name, AdminRoleName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(request.Name.Trim(), role.Name, StringComparison.Ordinal))
            {
                return Fail(400, "The Admin role cannot be renamed");
            }

            var nameTaken = await _db.Roles
                .AnyAsync(r => r.Name.ToLower() == request.Name.Trim().ToLower() && r.Id != request.Id, cancellationToken);
            if (nameTaken)
            {
                return Fail(400, "A role with this name already exists");
            }

            var permissionIds = request.PermissionIds.Distinct().ToList();
            if (permissionIds.Count > 0)
            {
                permissionIds = await _db.Permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);
            }

            try
            {
                role.Name = request.Name.Trim();
                role.Description = request.Description;

                _db.RolePermissions.RemoveRange(role.RolePermissions);
                role.RolePermissions = permissionIds
                    .Select(pid => new RolePermission { RoleId = role.Id, PermissionId = pid })
                    .ToList();

                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Role updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role {RoleId}", request.Id);
                return Fail(500, "An error occurred while updating the role.");
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
