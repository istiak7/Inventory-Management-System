using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Users.Shared.Services;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Command.DeleteUser
{
    // Removes a user from the system. Sales, purchases and other records still point at who
    // made them, so the user is marked deleted (hidden, cannot log in) rather than erased.
    public class DeleteUserCommandHandler(
        AppDbContext _db,
        ICurrentUser _currentUser,
        ILogger<DeleteUserCommandHandler> _logger
    ) : IRequestHandler<DeleteUserCommand, Result>
    {
        public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            const int deleted = (int)EntityStatus.Deleted;
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == request.Id && u.IsActive != deleted, cancellationToken);
            if (user is null) return Fail(404, "User not found");

            if (user.Id == _currentUser.UserId)
                return Fail(400, "You cannot delete your own account");

            // The system must always keep one active admin, or nobody could manage users again.
            var isAdmin = string.Equals(user.Role.Name, MenuActionAccess.AdminRoleName, StringComparison.OrdinalIgnoreCase);
            if (isAdmin && user.IsActive == (int)EntityStatus.Active)
            {
                var otherActiveAdmins = await _db.Users.CountAsync(u =>
                    u.Id != user.Id && u.RoleId == user.RoleId && u.IsActive == (int)EntityStatus.Active,
                    cancellationToken);
                if (otherActiveAdmins == 0)
                    return Fail(400, "This is the last active admin. Make another user admin first.");
            }

            try
            {
                user.Delete();
                user.UpDatedAt = DateTime.UtcNow;
                // Signed out everywhere: the refresh tokens stop working at once.
                await RefreshTokenStore.RevokeAllAsync(_db, user.Id, keepRefreshToken: null, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "User deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", request.Id);
                return Fail(500, "An error occurred while deleting the user.");
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
