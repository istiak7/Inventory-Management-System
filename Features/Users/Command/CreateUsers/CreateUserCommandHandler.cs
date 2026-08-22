using MediatR;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Users.Command.Events;
using Inventory_Management_System.Shared;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Command.CreateUsers
{
    public class CreateUserCommandHandler(
        AppDbContext _db,
        IMediator _mediator,
        ILogger<CreateUserCommandHandler> _logger)
    : IRequestHandler<CreateUserCommand, Result>
    {
        private static string NewRefreshToken => Guid.NewGuid().ToString();

        public async Task<Result> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var emailTaken = await _db.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);
            if (emailTaken)
            {
                return Fail(400, "User already exists");
            }

            var roleExists = await _db.Roles
                .AnyAsync(r => r.Id == request.RoleId, cancellationToken);
            if (!roleExists)
            {
                return Fail(400, "Selected role does not exist");
            }

            // Only staff-style users are tied to a branch. Admin keeps BranchId null (all branches).
            if (request.BranchId.HasValue)
            {
                var branchExists = await _db.Branches
                    .AnyAsync(b => b.Id == request.BranchId.Value, cancellationToken);
                if (!branchExists)
                {
                    return Fail(400, "Selected branch does not exist");
                }
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
                    Name = request.Username,
                    Email = request.Email,
                    PasswordHash = request.Password,
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
