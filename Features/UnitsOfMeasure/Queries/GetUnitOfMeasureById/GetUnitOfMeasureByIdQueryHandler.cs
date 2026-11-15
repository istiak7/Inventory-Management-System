using Inventory_Management_System.Database;
using Inventory_Management_System.Features.UnitsOfMeasure.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Queries.GetUnitOfMeasureById
{
    public class GetUnitOfMeasureByIdQueryHandler(
        AppDbContext _db,
        ILogger<GetUnitOfMeasureByIdQueryHandler> _logger
    ) : IRequestHandler<GetUnitOfMeasureByIdQuery, Result>
    {
        public async Task<Result> Handle(GetUnitOfMeasureByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                const int active = (int)EntityStatus.Active;
                var response = await _db.UnitOfMeasures.AsNoTracking()
                    .Where(u => u.Id == request.Id)
                    .Select(u => new UnitOfMeasureResponse(
                        u.Id, u.Name, u.ShortName, u.Description,
                        u.IsActive == active, u.Products.Count(), u.CreatedAt))
                    .FirstOrDefaultAsync(cancellationToken);

                if (response is null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Not Found", Message = $"Unit of measurement with id {request.Id} was not found." };

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Unit of measurement retrieved successfully", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unit of measurement {Id}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving the unit of measurement." };
            }
        }
    }
}
