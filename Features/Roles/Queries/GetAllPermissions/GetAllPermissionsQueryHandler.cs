using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Roles.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Roles.Queries.GetAllPermissions
{
    public class GetAllPermissionsQueryHandler(
        AppDbContext _db,
        ILogger<GetAllPermissionsQueryHandler> _logger
    ) : IRequestHandler<GetAllPermissionsQuery, Result>
    {
        public async Task<Result> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var permissions = await _db.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive != (int)EntityStatus.Deleted)
                    .OrderBy(p => p.Name)
                    .Select(p => new PermissionResponse(p.Id, p.Name, p.Description))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Permissions retrieved successfully",
                    Data = permissions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving permissions."
                };
            }
        }
    }
}
