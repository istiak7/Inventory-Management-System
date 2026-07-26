using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Branches.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Branches.Queries.GetBranchStock
{
    public class GetBranchStockQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetBranchStockQueryHandler> _logger
    ) : IRequestHandler<GetBranchStockQuery, Result>
    {
        public async Task<Result> Handle(GetBranchStockQuery request, CancellationToken cancellationToken)
        {
            if (request.BranchId <= 0)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "A valid branch id is required." };

            try
            {
                var branch = await _dbContext.Branches
                    .AsNoTracking()
                    .Where(b => b.Id == request.BranchId)
                    .Select(b => new { b.Id, b.Name })
                    .FirstOrDefaultAsync(cancellationToken);
                if (branch is null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"Branch {request.BranchId} not found." };

                var items = await _dbContext.Stocks
                    .AsNoTracking()
                    .Where(s => s.BranchId == request.BranchId)
                    .OrderBy(s => s.ProductVariantId)
                    .Select(s => new BranchStockItem(
                        s.ProductVariantId,
                        s.ProductVariant.SKU,
                        s.ProductVariant.Product.ProductName,
                        s.ProductVariant.IsSerialized,
                        s.CurrentStock))
                    .ToListAsync(cancellationToken);

                var response = new BranchStockResponse(
                    branch.Id,
                    branch.Name,
                    items.Sum(i => i.CurrentStock),
                    items.Count,
                    items);

                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Branch stock retrieved successfully", Data = response };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock for branch {BranchId}", request.BranchId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving branch stock." };
            }
        }
    }
}
