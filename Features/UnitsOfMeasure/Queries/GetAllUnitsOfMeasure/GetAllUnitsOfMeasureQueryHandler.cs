using Inventory_Management_System.Database;
using Inventory_Management_System.Features.UnitsOfMeasure.Shared.Dtos;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.PaginationExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Queries.GetAllUnitsOfMeasure
{
    public class GetAllUnitsOfMeasureQueryHandler(
        AppDbContext _db,
        ILogger<GetAllUnitsOfMeasureQueryHandler> _logger
    ) : IRequestHandler<GetAllUnitsOfMeasureQuery, Result>
    {
        public async Task<Result> Handle(GetAllUnitsOfMeasureQuery request, CancellationToken cancellationToken)
        {
            try
            {
                const int active = (int)EntityStatus.Active;
                const int inactive = (int)EntityStatus.InActive;

                var query = _db.UnitOfMeasures.AsNoTracking()
                    .Where(u => u.IsActive == active || u.IsActive == inactive);

                switch (request.Status?.Trim().ToLowerInvariant())
                {
                    case "active": query = query.Where(u => u.IsActive == active); break;
                    case "inactive": query = query.Where(u => u.IsActive == inactive); break;
                }

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(u => EF.Functions.ILike(u.Name, term) || EF.Functions.ILike(u.ShortName, term));
                }

                var paged = await query
                    .OrderBy(u => u.Name)
                    .Select(u => new UnitOfMeasureResponse(
                        u.Id, u.Name, u.ShortName, u.Description,
                        u.IsActive == active,
                        u.Products.Count(),
                        u.CreatedAt))
                    .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Units of measurement retrieved successfully",
                    Data = paged,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving units of measurement");
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving units of measurement.",
                };
            }
        }
    }
}
