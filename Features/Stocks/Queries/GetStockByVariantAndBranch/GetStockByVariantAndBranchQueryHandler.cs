using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Stocks.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariantAndBranch
{
    public class GetStockByVariantAndBranchQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetStockByVariantAndBranchQueryHandler> _logger
    ) : IRequestHandler<GetStockByVariantAndBranchQuery, Result>
    {
        public async Task<Result> Handle(GetStockByVariantAndBranchQuery request, CancellationToken cancellationToken)
        {
            if (request.ProductVariantId <= 0 || request.BranchId <= 0)
                return new Result { IsSuccess = false, StatusCode = 400, Status = "Error", Message = "A valid product variant id and branch id are required." };

            try
            {
                var stock = await _dbContext.Stocks
                    .AsNoTracking()
                    .Where(s => s.ProductVariantId == request.ProductVariantId && s.BranchId == request.BranchId)
                    .Select(s => new StockResponse(
                        s.Id,
                        s.BranchId,
                        s.Branch.Name,
                        s.ProductVariantId,
                        s.ProductVariant.SKU,
                        s.ProductVariant.Product.ProductName,
                        s.ProductVariant.IsSerialized,
                        s.CurrentStock))
                    .FirstOrDefaultAsync(cancellationToken);

                if (stock is not null)
                    return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "Stock retrieved successfully", Data = stock };

                // No stock row yet — return a zero-stock response as long as the variant & branch are valid.
                var variant = await _dbContext.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.Id == request.ProductVariantId)
                    .Select(v => new { v.Id, v.SKU, v.IsSerialized, ProductName = v.Product.ProductName })
                    .FirstOrDefaultAsync(cancellationToken);
                if (variant is null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"Product variant {request.ProductVariantId} not found." };

                var branch = await _dbContext.Branches
                    .AsNoTracking()
                    .Where(b => b.Id == request.BranchId)
                    .Select(b => new { b.Id, b.Name })
                    .FirstOrDefaultAsync(cancellationToken);
                if (branch is null)
                    return new Result { IsSuccess = false, StatusCode = 404, Status = "Error", Message = $"Branch {request.BranchId} not found." };

                var zero = new StockResponse(0, branch.Id, branch.Name, variant.Id, variant.SKU, variant.ProductName, variant.IsSerialized, 0);
                return new Result { IsSuccess = true, StatusCode = 200, Status = "Success", Message = "No stock yet for this variant at this branch", Data = zero };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock for variant {VariantId} at branch {BranchId}", request.ProductVariantId, request.BranchId);
                return new Result { IsSuccess = false, StatusCode = 500, Status = "Error", Message = "An error occurred while retrieving stock." };
            }
        }
    }
}
