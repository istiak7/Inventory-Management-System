using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Warranty.Shared;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Warranty.Queries.GetWarrantyClaimById
{
    public class GetWarrantyClaimByIdQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetWarrantyClaimByIdQueryHandler> _logger
    ) : IRequestHandler<GetWarrantyClaimByIdQuery, Result>
    {
        public async Task<Result> Handle(GetWarrantyClaimByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var row = await _dbContext.WarrantyClaims
                    .AsNoTracking()
                    .Where(c => c.Id == request.Id)
                    .ProjectToRow()
                    .FirstOrDefaultAsync(cancellationToken);

                if (row == null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = "Warranty claim not found." };

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Warranty claim retrieved successfully",
                    Data = row.ToResponse()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving warranty claim {ClaimId}", request.Id);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving the warranty claim." };
            }
        }
    }
}
