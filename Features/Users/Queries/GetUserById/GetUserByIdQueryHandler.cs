using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Users.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler(
        AppDbContext _db,
        ILogger<GetUserByIdQueryHandler> _logger
    ) : IRequestHandler<GetUserByIdQuery, Result>
    {
        public async Task<Result> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == request.Id)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Email,
                        u.RoleId,
                        RoleName = u.Role != null ? u.Role.Name : string.Empty,
                        u.BranchId,
                        BranchName = u.Branch != null ? u.Branch.Name : null,
                        u.IsActive,
                        u.CreatedAt,
                        DirectPermissionIds = u.UserPermissions.Select(up => up.PermissionId).ToList()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (user is null)
                {
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "User not found"
                    };
                }

                // Effective access = permissions the role grants + permissions given directly to the user.
                var rolePermissionIds = await _db.RolePermissions
                    .Where(rp => rp.RoleId == user.RoleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync(cancellationToken);

                var allPermissionIds = rolePermissionIds
                    .Union(user.DirectPermissionIds)
                    .ToList();

                var permissions = await _db.Permissions
                    .Where(p => allPermissionIds.Contains(p.Id))
                    .OrderBy(p => p.Name)
                    .Select(p => new PermissionResponse(p.Id, p.Name, p.Description))
                    .ToListAsync(cancellationToken);

                var response = new UserDetailResponse(
                    user.Id,
                    user.Name,
                    user.Email,
                    user.RoleId,
                    user.RoleName,
                    user.BranchId,
                    user.BranchName,
                    user.IsActive,
                    user.CreatedAt,
                    permissions,
                    user.DirectPermissionIds);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "User retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", request.Id);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving the user."
                };
            }
        }
    }
}
