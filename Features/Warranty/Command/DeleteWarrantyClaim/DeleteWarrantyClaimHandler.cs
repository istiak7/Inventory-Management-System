using Inventory_Management_System.Database;
using Inventory_Management_System.Entities;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Shared;
using Inventory_Management_System.Shared.Extensions.LockExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Command.DeleteWarrantyClaim
{
    // Removes a claim opened by mistake. Only an Open claim: once repair has started the claim is
    // the record of the unit being with us, and it is closed by Reject instead.
    public class DeleteWarrantyClaimHandler(
        AppDbContext _dbContext,
        ILogger<DeleteWarrantyClaimHandler> _logger
    ) : IRequestHandler<DeleteWarrantyClaimCommand, Result>
    {
        public async Task<Result> Handle(DeleteWarrantyClaimCommand request, CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _dbContext.LockRowAsync<WarrantyClaim>(request.WarrantyClaimId, cancellationToken);

                var claim = await _dbContext.WarrantyClaims
                    .FirstOrDefaultAsync(c => c.Id == request.WarrantyClaimId, cancellationToken);

                if (claim == null)
                    return Error(404, "Warranty claim not found.");

                if (claim.Status != WarrantyClaimStatus.Open)
                    return Error(400, $"Claim {claim.ClaimNumber} is {claim.Status}; only an open claim can be deleted. Reject it instead.");

                _dbContext.WarrantyClaims.Remove(claim);

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = $"Claim {claim.ClaimNumber} deleted successfully" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error deleting warranty claim {ClaimId}", request.WarrantyClaimId);
                return Error(500, "An error occurred while deleting the warranty claim.");
            }
        }

        private static Result Error(int statusCode, string message) =>
            new() { IsSuccess = false, StatusCode = statusCode, Status = "Error", Message = message };
    }
}
