using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Users.Command.Events;
using Inventory_Management_System.Features.Users.Shared;
using Inventory_Management_System.Shared;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Command.CreateUsers
{
    public class CreateUserCommandHandler(
        AppDbContext _db,
        IMediator _mediator,
        ICurrentUser _currentUser,
        MenuActionAccess _access,
        ILogger<CreateUserCommandHandler> _logger)
    : IRequestHandler<CreateUserCommand, Result>
    {
        private static string NewRefreshToken => Guid.NewGuid().ToString();

        public async Task<Result> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            // Emails are stored in lower case, so "Ali@x.com" and "ali@x.com" are the same user.
            var email = request.Email.Trim().ToLowerInvariant();
            var username = request.Username.Trim();

            var emailTaken = await _db.Users
                .AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
            if (emailTaken)
            {
                return Fail(400, "A user with this email already exists");
            }

            if (await UserAccessRules.NameTakenAsync(_db, username, null, cancellationToken))
            {
                return Fail(400, "A user with this user name already exists");
            }

            var role = await UserAccessRules.FindRoleAsync(_db, request.RoleId, cancellationToken);
            if (role is null)
            {
                return Fail(400, "Selected role does not exist");
            }

            var notAllowed = UserAccessRules.CheckAdminRoleChange(_currentUser, null, role);
            if (notAllowed is not null)
            {
                return Fail(403, notAllowed);
            }

            // Only the Admin role keeps BranchId null (all branches); everyone else gets one branch.
            var branchError = await UserAccessRules.CheckBranchAsync(_db, role, request.BranchId, cancellationToken);
            if (branchError is not null)
            {
                return Fail(400, branchError);
            }

            // Keep only permission ids that actually exist, and drop duplicates.
            var permissionIds = request.PermissionIds.Distinct().ToList();
            if (permissionIds.Count > 0)
            {
                var validIds = await _db.Permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);
                permissionIds = validIds;
            }

            try
            {
                var user = new User
                {
                    Name = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RoleId = request.RoleId,
                    BranchId = request.BranchId,
                    IsActive = (int)EntityStatus.Active,
                    RefreshToken = NewRefreshToken,
                    RefreshTokenExpireTime = DateTime.UtcNow.AddDays(7),
                    UserPermissions = permissionIds
                        .Select(pid => new UserPermission { PermissionId = pid })
                        .ToList()
                };

                await _db.Users.AddAsync(user, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                _access.InvalidateUser(user.Id);

                await _mediator.Publish(
                    new UserRegistrationEvent(user.Id, request.Username, request.Email),
                    cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "User created successfully",
                    Data = new { user.Id }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return Fail(500, "An error occurred while creating the user.");
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
