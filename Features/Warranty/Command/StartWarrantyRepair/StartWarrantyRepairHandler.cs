using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Command.StartWarrantyRepair
{
    public class StartWarrantyRepairHandler(
        AppDbContext _dbContext,
        ILogger<StartWarrantyRepairHandler> _logger
    ) : IRequestHandler<StartWarrantyRepairCommand, Result>
    {
        public async Task<Result> Handle(StartWarrantyRepairCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var claim = await _dbContext.WarrantyClaims
                    .FirstOrDefaultAsync(c => c.Id == request.WarrantyClaimId, cancellationToken);

                if (claim == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Warranty claim not found." };

                if (claim.Status != WarrantyClaimStatus.Open)
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Only an open claim can be sent for repair. Claim {claim.ClaimNumber} is {claim.Status}." };

                claim.Status = WarrantyClaimStatus.InRepair;
                claim.RepairStartedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(request.TechnicianName))
                    claim.TechnicianName = request.TechnicianName.Trim();

                await _dbContext.SaveChangesAsync(cancellationToken);

                var row = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.Id == claim.Id)
                    .ProjectToRow()
                    .FirstAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = $"Claim {claim.ClaimNumber} is now in repair", Data = row.ToResponse() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting repair on warranty claim {ClaimId}", request.WarrantyClaimId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while starting the repair." };
            }
        }
    }
}
