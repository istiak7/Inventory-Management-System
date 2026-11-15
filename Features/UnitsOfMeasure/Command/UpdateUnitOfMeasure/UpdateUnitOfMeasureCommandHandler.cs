using Inventory_Management_System.Database;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.UpdateUnitOfMeasure
{
    public class UpdateUnitOfMeasureCommandHandler(
        AppDbContext _db,
        ILogger<UpdateUnitOfMeasureCommandHandler> _logger
    ) : IRequestHandler<UpdateUnitOfMeasureCommand, Result>
    {
        public async Task<Result> Handle(UpdateUnitOfMeasureCommand request, CancellationToken cancellationToken)
        {
            var unit = await _db.UnitOfMeasures.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
            if (unit is null) return Fail(404, $"Unit of measurement with id {request.Id} was not found.");

            var name = request.Name.Trim();
            var shortName = request.ShortName.Trim();

            if (await _db.UnitOfMeasures.AnyAsync(u => u.Id != unit.Id && u.Name.ToLower() == name.ToLower(), cancellationToken))
                return Fail(400, $"A unit named '{name}' already exists.");
            if (await _db.UnitOfMeasures.AnyAsync(u => u.Id != unit.Id && u.ShortName.ToLower() == shortName.ToLower(), cancellationToken))
                return Fail(400, $"A unit with short name '{shortName}' already exists.");

            try
            {
                unit.Name = name;
                unit.ShortName = shortName;
                // Not sent (null) keeps what is stored; an empty string clears it.
                if (request.Description is not null) unit.Description = request.Description.Trim();
                // Making a unit inactive only stops it being picked for new products;
                // products that already use it keep it.
                unit.IsActive = request.IsActive ? (int)EntityStatus.Active : (int)EntityStatus.InActive;
                unit.UpDatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Unit of measurement updated successfully",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating unit of measurement {Id}", request.Id);
                return Fail(500, "An error occurred while updating the unit of measurement.");
            }
        }

        private static Result Fail(int statusCode, string message) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Status = "Error",
            Message = message,
        };
    }
}
