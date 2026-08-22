using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Users.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.Users.Queries.GetAllUsers
{
    public class GetAllUsersQueryHandler(
        AppDbContext _db,
        ILogger<GetAllUsersQueryHandler> _logger
    ) : IRequestHandler<GetAllUsersQuery, Result>
    {
        public async Task<Result> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagedResult = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.IsActive != (int)EntityStatus.Deleted)
                    .OrderBy(u => u.Id)
                    .Select(u => new UserResponse(
                        u.Id,
                        u.Name,
                        u.Email,
                        u.RoleId,
                        u.Role != null ? u.Role.Name : string.Empty,
                        u.BranchId,
                        u.Branch != null ? u.Branch.Name : null,
                        u.IsActive,
                        u.CreatedAt))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Users retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving users."
                };
            }
        }
    }
}
