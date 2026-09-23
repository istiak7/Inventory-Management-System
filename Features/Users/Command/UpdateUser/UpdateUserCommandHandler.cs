using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Users.Command.UpdateUser
{
    public class UpdateUserCommandHandler(
        AppDbContext _db,
        ILogger<UpdateUserCommandHandler> _logger
    ) : IRequestHandler<UpdateUserCommand, Result>
    {
        public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Include(u => u.UserPermissions)
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

            if (user is null)
            {
                return Fail(404, "User not found");
            }

            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == request.RoleId, cancellationToken);
            if (!roleExists)
            {
                return Fail(400, "Selected role does not exist");
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
                user.Name = request.Username;
                user.RoleId = request.RoleId;
                user.BranchId = request.BranchId;
                user.IsActive = request.IsActive;

                if (!string.IsNullOrEmpty(request.NewPassword))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

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
