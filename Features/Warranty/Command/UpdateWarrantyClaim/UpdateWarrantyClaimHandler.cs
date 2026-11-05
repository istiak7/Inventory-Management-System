using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Command.UpdateWarrantyClaim
{
    public class UpdateWarrantyClaimHandler(
        AppDbContext _dbContext,
        ILogger<UpdateWarrantyClaimHandler> _logger
    ) : IRequestHandler<UpdateWarrantyClaimCommand, Result>
    {
        public async Task<Result> Handle(UpdateWarrantyClaimCommand request, CancellationToken cancellationToken)
        {
            // Lock the claim so a status change at the same moment waits for this edit.
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<WarrantyClaim>(request.Id, cancellationToken);

                var claim = await _dbContext.WarrantyClaims
                    .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

                if (claim == null)
                    return Error(404, "Warranty claim not found.");

                if (!claim.IsOpen)
                    return Error(400, $"Claim {claim.ClaimNumber} is {claim.Status} and can no longer be edited.");

                claim.DefectDescription = request.DefectDescription.Trim();
                claim.AccessoriesReceived = string.IsNullOrWhiteSpace(request.AccessoriesReceived) ? null : request.AccessoriesReceived.Trim();
                claim.TechnicianName = string.IsNullOrWhiteSpace(request.TechnicianName) ? null : request.TechnicianName.Trim();
                claim.UpDatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var row = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.Id == claim.Id)
                    .ProjectToRow()
                    .FirstAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = $"Claim {claim.ClaimNumber} updated successfully",
                    Data = row.ToResponse()
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error updating warranty claim {ClaimId}", request.Id);
                return Error(500, "An error occurred while updating the warranty claim.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
