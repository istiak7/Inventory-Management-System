using Inventory_Management_System.Database;
using Inventory_Management_System.Entities.Common;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Reports.Queries.GetDailyGrossProfit
{
    public class GetDailyGrossProfitQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetDailyGrossProfitQueryHandler> _logger
    ) : IRequestHandler<GetDailyGrossProfitQuery, Result>
    {
        public async Task<Result> Handle(GetDailyGrossProfitQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // A shop calendar day (Dhaka time), turned into the UTC moments it starts and ends.
                var day = (request.Date ?? BusinessClock.Today).Date;
                var dayStart = BusinessClock.StartOfDayUtc(day);
                var nextDayStart = BusinessClock.StartOfDayUtc(day.AddDays(1));

                var sales = _dbContext.CustomerSales
                    .AsNoTracking()
                    .Where(s => s.SaleDate >= dayStart && s.SaleDate < nextDayStart)
                    .WhereCountsTowardProfit();

                if (request.BranchId is int branchId)
                    sales = sales.Where(s => s.BranchId == branchId);

                var totals = await sales.ToGrossProfitTotalsAsync(cancellationToken);

                var response = new DailyGrossProfitResponse(
                    DateOnly.FromDateTime(day),
                    totals.Revenue,
                    totals.CostOfGoodsSold,
                    totals.GrossProfit,
                    GrossProfitMath.MarginPercent(totals.Revenue, totals.GrossProfit),
                    totals.SalesCount,
                    totals.UnitsSold);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Daily gross profit retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating daily gross profit for {Date}", request.Date);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while calculating daily gross profit."
                };
            }
        }
    }
}
