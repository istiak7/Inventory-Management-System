using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static Inventory_Management_System.Entities.Common.EntityConstant;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Command.CreateUnitOfMeasure
{
    public class CreateUnitOfMeasureCommandHandler(
        AppDbContext _db,
        ILogger<CreateUnitOfMeasureCommandHandler> _logger
    ) : IRequestHandler<CreateUnitOfMeasureCommand, Result>
    {
        public async Task<Result> Handle(CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name.Trim();
            var shortName = request.ShortName.Trim();

            // Names are compared ignoring case: "Kg" and "kg" are the same unit.
            if (await _db.UnitOfMeasures.AnyAsync(u => u.Name.ToLower() == name.ToLower(), cancellationToken))
                return Fail(400, $"A unit named '{name}' already exists.");
            if (await _db.UnitOfMeasures.AnyAsync(u => u.ShortName.ToLower() == shortName.ToLower(), cancellationToken))
                return Fail(400, $"A unit with short name '{shortName}' already exists.");

            try
            {
                var unit = new UnitOfMeasure
                {
                    Name = name,
                    ShortName = shortName,
                    Description = request.Description?.Trim() ?? string.Empty,
                    IsActive = request.IsActive ? (int)EntityStatus.Active : (int)EntityStatus.InActive,
                    CreatedAt = DateTime.UtcNow,
                };
                _db.UnitOfMeasures.Add(unit);
                await _db.SaveChangesAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 201,
                    Status = "Success",
                    Message = "Unit of measurement created successfully",
                    Data = new { unit.Id },
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating unit of measurement");
                return Fail(500, "An error occurred while creating the unit of measurement.");
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
