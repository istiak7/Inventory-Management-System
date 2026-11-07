using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Purchases.Shared;
using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.CheckSerialNumbers
{
    public class CheckSerialNumbersQueryHandler(AppDbContext _dbContext)
        : IRequestHandler<CheckSerialNumbersQuery, Result>
    {
        public async Task<Result> Handle(CheckSerialNumbersQuery request, CancellationToken cancellationToken)
        {
            var serials = PurchaseSerials.Clean(request.SerialNumbers).Distinct().ToList();
            var existing = await PurchaseSerials.FindExistingAsync(
                _dbContext, serials, request.ExcludePurchaseOrderId, cancellationToken);

            return new Result
            {
                IsSuccess = true,
                StatusCode = 200,
                Status = "Success",
                Message = existing.Count == 0 ? "All serial numbers are available." : "Some serial numbers already exist.",
                Data = new CheckSerialNumbersResponse(existing)
            };
        }
    }
}
