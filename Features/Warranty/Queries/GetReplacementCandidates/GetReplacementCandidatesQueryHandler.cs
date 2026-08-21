using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Warranty.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Queries.GetReplacementCandidates
{
    public class GetReplacementCandidatesQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetReplacementCandidatesQueryHandler> _logger
    ) : IRequestHandler<GetReplacementCandidatesQuery, Result>
    {
        public async Task<Result> Handle(GetReplacementCandidatesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var claim = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.Id == request.WarrantyClaimId)
                    .Select(c => new { c.Id, c.BranchId, c.ProductSerialId, c.ProductSerial.ProductVariantId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (claim == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Warranty claim not found." };

                var query = _dbContext.ProductSerials
                    .AsNoTracking()
                    .Where(s => s.ProductVariantId == claim.ProductVariantId
                             && s.BranchId == claim.BranchId
                             && s.Status == SerialStatus.InStock
                             && s.Id != claim.ProductSerialId);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(s => EF.Functions.ILike(s.SerialNumber, term));
                }

                var candidates = await query
                    .OrderBy(s => s.ReceivedDate)
                    .ThenBy(s => s.Id)
                    .Take(100)
                    .Select(s => new ReplacementCandidateResponse(
                        s.Id,
                        s.SerialNumber,
                        s.ProductVariantId,
                        s.ProductVariant.SKU,
                        s.ProductVariant.Product.ProductName,
                        s.BranchId,
                        s.Branch.Name,
                        s.ReceivedDate,
                        s.WarrantyMonths))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = candidates.Count == 0
                        ? "No matching unit is in stock at this branch to use as a replacement."
                        : "Replacement candidates retrieved successfully",
                    Data = candidates
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving replacement candidates for claim {ClaimId}", request.WarrantyClaimId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving replacement candidates." };
            }
        }
    }
}
