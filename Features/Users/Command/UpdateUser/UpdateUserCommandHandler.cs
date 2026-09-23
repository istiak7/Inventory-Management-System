using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Command.UpdateUser
{
    public class UpdateUserCommandHandler(
        AppDbContext _db,
        ILogger<UpdateUserCommandHandler> _logger
    ) : IRequestHandler<UpdateUserCommand, Result>
    {
        private const string AdminRoleName = "Admin";

        public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Include(u => u.UserPermissions)
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (user is null)
            {
                return Fail(404, "User not found");
            }

            var newRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
            if (newRole is null)
            {
                return Fail(400, "Selected role does not exist");
            }

            var username = request.Username.Trim();
            var nameTaken = await _db.Users
                .AnyAsync(u => u.Name == username && u.Id != user.Id, cancellationToken);
            if (nameTaken)
            {
                return Fail(400, "A user with this user name already exists");
            }

            // The system must always keep one active admin, or nobody could manage users again.
            var adminRoleId = await _db.Roles
                .Where(r => r.Name == AdminRoleName)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            var isActiveAdminNow = user.RoleId == adminRoleId && user.IsActive == (int)EntityStatus.Active;
            var staysActiveAdmin = request.RoleId == adminRoleId && request.IsActive == (int)EntityStatus.Active;
            if (isActiveAdminNow && !staysActiveAdmin)
            {
                var otherActiveAdmins = await _db.Users.CountAsync(u =>
                    u.Id != user.Id && u.RoleId == adminRoleId && u.IsActive == (int)EntityStatus.Active,
                    cancellationToken);
                if (otherActiveAdmins == 0)
                {
                    return Fail(400, "This is the last active admin. Make another user admin first.");
                }
            }

            if (request.BranchId.HasValue)
            {
                var branchExists = await _db.Branches
                    .AnyAsync(b => b.Id == request.BranchId.Value, cancellationToken);
                if (!branchExists)
                {
                    return Fail(400, "Selected branch does not exist");
                }
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
                user.Name = username;
                user.RoleId = request.RoleId;
                user.BranchId = request.BranchId;
                user.IsActive = request.IsActive;

                if (!string.IsNullOrEmpty(request.NewPassword))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // A deactivated user, or one whose password was reset, is signed out everywhere.
                if (!string.IsNullOrEmpty(request.NewPassword) || request.IsActive != (int)EntityStatus.Active)
                    await RefreshTokenStore.RevokeAllAsync(_db, user.Id, keepRefreshToken: null, cancellationToken);

                // Replace the direct permissions with the new set.
                _db.UserPermissions.RemoveRange(user.UserPermissions);
                user.UserPermissions = permissionIds
                    .Select(pid => new UserPermission { UserId = user.Id, PermissionId = pid })
                    .ToList();

                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "User updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", request.Id);
                return Fail(500, "An error occurred while updating the user.");
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
