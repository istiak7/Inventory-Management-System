using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Users.Shared;
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
        MenuActionAccess _access,
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

            var notAllowed = UserAccessRules.CheckAdminRoleChange(_currentUser, user.Role, null);
            if (notAllowed is not null)
                return Fail(403, notAllowed);

            // The system must always keep one active admin, or nobody could manage users again.
            if (await UserAccessRules.IsLastActiveAdminAsync(_db, user, user.Role, cancellationToken))
                return Fail(400, UserAccessRules.LastAdminMessage);

            try
            {
                user.Delete();
                user.UpDatedAt = DateTime.UtcNow;
                // Signed out everywhere: the refresh tokens stop working at once.
                await RefreshTokenStore.RevokeAllAsync(_db, user.Id, keepRefreshToken: null, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                // Their access token stops working on its next request too.
                _access.InvalidateUser(user.Id);

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
