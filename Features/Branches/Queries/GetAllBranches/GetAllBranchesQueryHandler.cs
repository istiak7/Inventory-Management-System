using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Branches.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Branches.Queries.GetAllBranches
{
    public class GetAllBranchesQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAllBranchesQueryHandler> _logger
    ) : IRequestHandler<GetAllBranchesQuery, Result>
    {
        public async Task<Result> Handle(GetAllBranchesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagedResult = await _dbContext.Branches
                    .AsNoTracking()
                    .OrderBy(b => b.Id)
                    .Select(b => new BranchResponse(
                        b.Id,
                        b.Name,
                        b.Location,
                        b.PhoneNumber,
                        b.Email))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Branches retrieved successfully",
                    Data = pagedResult
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branches");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving branches."
                };
            }
        }
    }
}
