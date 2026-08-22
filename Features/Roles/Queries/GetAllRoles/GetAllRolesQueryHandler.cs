using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Roles.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Roles.Queries.GetAllRoles
{
    public class GetAllRolesQueryHandler(
        AppDbContext _db,
        ILogger<GetAllRolesQueryHandler> _logger
    ) : IRequestHandler<GetAllRolesQuery, Result>
    {
        public async Task<Result> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var roles = await _db.Roles
                    .AsNoTracking()
                    .Where(r => r.IsActive != (int)EntityStatus.Deleted)
                    .OrderBy(r => r.Id)
                    .Select(r => new RoleResponse(
                        r.Id,
                        r.Name,
                        r.Description,
                        _db.Users.Count(u => u.RoleId == r.Id && u.IsActive != (int)EntityStatus.Deleted),
                        r.RolePermissions.Select(rp => rp.PermissionId).ToList(),
                        r.RolePermissions.Count))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Roles retrieved successfully",
                    Data = roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving roles."
                };
            }
        }
    }
}
