using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Command.DeliverWarrantyClaim
{
    public class DeliverWarrantyClaimHandler(
        AppDbContext _dbContext,
        ILogger<DeliverWarrantyClaimHandler> _logger
    ) : IRequestHandler<DeliverWarrantyClaimCommand, Result>
    {
        public async Task<Result> Handle(DeliverWarrantyClaimCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var claim = await _dbContext.WarrantyClaims
                    .FirstOrDefaultAsync(c => c.Id == request.WarrantyClaimId, cancellationToken);

                if (claim == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Warranty claim not found." };

                if (claim.Status is not (WarrantyClaimStatus.Resolved or WarrantyClaimStatus.Rejected))
                    return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = $"Claim {claim.ClaimNumber} is {claim.Status}. Only a resolved or rejected claim can be handed back to the customer." };

                claim.Status = WarrantyClaimStatus.Delivered;
                claim.DeliveredAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(request.Notes))
                    claim.ResolutionNotes = string.IsNullOrWhiteSpace(claim.ResolutionNotes)
                        ? request.Notes.Trim()
                        : $"{claim.ResolutionNotes}\nHandover: {request.Notes.Trim()}";

                await _dbContext.SaveChangesAsync(cancellationToken);

                var row = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.Id == claim.Id)
                    .ProjectToRow()
                    .FirstAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = $"Claim {claim.ClaimNumber} handed back to the customer", Data = row.ToResponse() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering warranty claim {ClaimId}", request.WarrantyClaimId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while closing the warranty claim." };
            }
        }
    }
}
