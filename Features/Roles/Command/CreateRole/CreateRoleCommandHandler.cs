using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Roles.Command.CreateRole
{
    public class CreateRoleCommandHandler(
        AppDbContext _db,
        ILogger<CreateRoleCommandHandler> _logger
    ) : IRequestHandler<CreateRoleCommand, Result>
    {
        public async Task<Result> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var nameTaken = await _db.Roles
                .AnyAsync(r => r.Name.ToLower() == request.Name.Trim().ToLower(), cancellationToken);
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
                var role = new Role
                {
                    Name = request.Name,
                    Description = request.Description,
                    RolePermissions = permissionIds
                        .Select(pid => new RolePermission { PermissionId = pid })
                        .ToList()
                };

                await _db.Roles.AddAsync(role, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Role created successfully",
                    Data = new { role.Id }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return Fail(500, "An error occurred while creating the role.");
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
