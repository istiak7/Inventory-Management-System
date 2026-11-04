using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Users.Shared;
using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Command.UpdateUser
{
    public class UpdateUserCommandHandler(
        AppDbContext _db,
        ICurrentUser _currentUser,
        MenuActionAccess _access,
        ILogger<UpdateUserCommandHandler> _logger
    ) : IRequestHandler<UpdateUserCommand, Result>
    {
        public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .Include(u => u.UserPermissions)
                .FirstOrDefaultAsync(u => u.Id == request.Id && u.IsActive != (int)EntityStatus.Deleted, cancellationToken);

            if (user is null)
            {
                return Fail(404, "User not found");
            }

            var newRole = await UserAccessRules.FindRoleAsync(_db, request.RoleId, cancellationToken);
            if (newRole is null)
            {
                return Fail(400, "Selected role does not exist");
            }

            var notAllowed = UserAccessRules.CheckAdminRoleChange(_currentUser, user.Role, newRole);
            if (notAllowed is not null)
            {
                return Fail(403, notAllowed);
            }

            // Nobody changes their own role or switches their own account off: it is how a user
            // would raise their own access, or lock themselves out by mistake.
            if (user.Id == _currentUser.UserId
                && (request.RoleId != user.RoleId || request.IsActive != user.IsActive))
            {
                return Fail(400, "You cannot change your own role or status. Ask another admin.");
            }

            var username = request.Username.Trim();
            if (await UserAccessRules.NameTakenAsync(_db, username, user.Id, cancellationToken))
            {
                return Fail(400, "A user with this user name already exists");
            }

            // The system must always keep one active admin, or nobody could manage users again.
            var staysActiveAdmin = UserAccessRules.IsAdminRole(newRole) && request.IsActive == (int)EntityStatus.Active;
            if (!staysActiveAdmin && await UserAccessRules.IsLastActiveAdminAsync(_db, user, user.Role, cancellationToken))
            {
                return Fail(400, UserAccessRules.LastAdminMessage);
            }

            var branchError = await UserAccessRules.CheckBranchAsync(_db, newRole, request.BranchId, cancellationToken);
            if (branchError is not null)
            {
                return Fail(400, branchError);
            }

            List<int>? permissionIds = null;
            if (request.PermissionIds is not null)
            {
                permissionIds = request.PermissionIds.Distinct().ToList();
                if (permissionIds.Count > 0)
                {
                    permissionIds = await _db.Permissions
                        .Where(p => permissionIds.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToListAsync(cancellationToken);
                }
            }

            try
            {
                var accessChanged = user.RoleId != request.RoleId
                    || user.BranchId != request.BranchId
                    || user.IsActive != request.IsActive;

                user.Name = username;
                user.RoleId = request.RoleId;
                user.BranchId = request.BranchId;
                user.IsActive = request.IsActive;

                if (!string.IsNullOrEmpty(request.NewPassword))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // A deactivated user, or one whose password was reset, is signed out everywhere.
                if (!string.IsNullOrEmpty(request.NewPassword) || request.IsActive != (int)EntityStatus.Active)
                    await RefreshTokenStore.RevokeAllAsync(_db, user.Id, keepRefreshToken: null, cancellationToken);

                // Replace the direct permissions with the new set (only when they were sent).
                if (permissionIds is not null)
                {
                    _db.UserPermissions.RemoveRange(user.UserPermissions);
                    user.UserPermissions = permissionIds
                        .Select(pid => new UserPermission { UserId = user.Id, PermissionId = pid })
                        .ToList();
                }

                await _db.SaveChangesAsync(cancellationToken);
                // The user's current token stops working on its next request, so they get the
                // new role/branch at once (or are logged out when deactivated).
                if (accessChanged) _access.InvalidateUser(user.Id);

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
