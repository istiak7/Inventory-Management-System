using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Command.RejectWarrantyClaim
{
    public class RejectWarrantyClaimHandler(
        AppDbContext _dbContext,
        ILogger<RejectWarrantyClaimHandler> _logger
    ) : IRequestHandler<RejectWarrantyClaimCommand, Result>
    {
        public async Task<Result> Handle(
            RejectWarrantyClaimCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var claim = await _dbContext.WarrantyClaims
                    .FirstOrDefaultAsync(
                    c => c.Id == request.WarrantyClaimId,
                    cancellationToken);

                if (claim == null)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 404,
                        Status = "Error",
                        Message = "Warranty claim not found."
                    };

                if (!claim.IsOpen)
                    return new Result
                    {
                        IsSuccess = false,
                        StatusCode = 400,
                        Status = "Error",
                        Message = $"Claim {claim.ClaimNumber} is already {claim.Status} and cannot be rejected."
                    };

                claim.Status = WarrantyClaimStatus.Rejected;
                claim.Resolution = WarrantyResolutionType.NotRepairable;
                claim.ResolutionNotes = request.Reason.Trim();
                claim.RejectedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

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
                    Message = $"Claim {claim.ClaimNumber} rejected",
                    Data = row.ToResponse()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting warranty claim {ClaimId}", request.WarrantyClaimId);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while rejecting the warranty claim."
                };
            }
        }
    }
}
