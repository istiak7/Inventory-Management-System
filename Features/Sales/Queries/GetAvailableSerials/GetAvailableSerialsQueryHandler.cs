using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Sales.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Sales.Queries.GetAvailableSerials
{
    public class GetAvailableSerialsQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetAvailableSerialsQueryHandler> _logger
    ) : IRequestHandler<GetAvailableSerialsQuery, Result>
    {
        public async Task<Result> Handle(GetAvailableSerialsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Only InStock units at this branch can be sold — the exact set CreateSale accepts.
                // The branch query filter in AppDbContext also keeps staff to their own branch.
                var query = _dbContext.ProductSerials
                    .AsNoTracking()
                    .Where(s => s.ProductVariantId == request.ProductVariantId
                             && s.BranchId == request.BranchId
                             && s.Status == SerialStatus.InStock);

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var term = $"%{request.Search.Trim()}%";
                    query = query.Where(s => EF.Functions.ILike(s.SerialNumber, term));
                }

                // Oldest stock first so sellers naturally move earlier units out first.
                // Capped at 100 — the seller searches to narrow down rather than scroll a huge list.
                var serials = await query
                    .OrderBy(s => s.ReceivedDate)
                    .ThenBy(s => s.Id)
                    .Take(100)
                    .Select(s => new AvailableSerialResponse(
                        s.Id,
                        s.SerialNumber,
                        s.WarrantyMonths,
                        s.ReceivedDate))
                    .ToListAsync(cancellationToken);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = serials.Count == 0
                        ? "No in-stock serial numbers for this product at this branch."
                        : "Available serial numbers retrieved successfully",
                    Data = serials
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available serials for variant {VariantId} at branch {BranchId}",
                    request.ProductVariantId, request.BranchId);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while retrieving available serial numbers."
                };
            }
        }
    }
}
