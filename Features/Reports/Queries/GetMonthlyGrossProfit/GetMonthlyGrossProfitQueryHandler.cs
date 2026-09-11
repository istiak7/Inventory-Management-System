using Inventory_Management_System.Database;
using Inventory_Management_System.Features.Reports.Shared;
using Inventory_Management_System.Features.Reports.Shared.Dtos;
using Inventory_Management_System.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory_Management_System.Features.Reports.Queries.GetMonthlyGrossProfit
{
    public class GetMonthlyGrossProfitQueryHandler(
        AppDbContext _dbContext,
        ILogger<GetMonthlyGrossProfitQueryHandler> _logger
    ) : IRequestHandler<GetMonthlyGrossProfitQuery, Result>
    {
        public async Task<Result> Handle(GetMonthlyGrossProfitQuery request, CancellationToken cancellationToken)
        {
            var today = DateTime.Now;
            var year = request.Year ?? today.Year;
            var month = request.Month ?? today.Month;

            if (month is < 1 or > 12)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Month must be between 1 and 12."
                };

            if (year is < 1 or > 9999)
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Status = "Error",
                    Message = "Year is out of range."
                };

            try
            {
                // Half-open range, so a sale timestamped late on the last day of the month is
                // still counted and one at midnight on the 1st of the next month is not.
                var monthStart = new DateTime(year, month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var sales = _dbContext.CustomerSales
                    .AsNoTracking()
                    .Where(s => s.SaleDate >= monthStart && s.SaleDate < monthEnd)
                    .WhereCountsTowardProfit();

                if (request.BranchId is int branchId)
                    sales = sales.Where(s => s.BranchId == branchId);

                // One pass, grouped by day. The month total is the sum of the days, so the
                // headline figure and the breakdown can never disagree.
                var byDay = await sales.ToDailyGrossProfitAsync(cancellationToken);

                var days = byDay
                    .Select(d => new DailyGrossProfitResponse(
                        d.Day,
                        d.Totals.Revenue,
                        d.Totals.CostOfGoodsSold,
                        d.Totals.GrossProfit,
                        GrossProfitMath.MarginPercent(d.Totals.Revenue, d.Totals.GrossProfit),
                        d.Totals.SalesCount,
                        d.Totals.UnitsSold))
                    .ToList();

                var revenue = GrossProfitMath.Round(days.Sum(d => d.Revenue));
                var cogs = GrossProfitMath.Round(days.Sum(d => d.CostOfGoodsSold));
                var grossProfit = GrossProfitMath.Round(revenue - cogs);

                var response = new MonthlyGrossProfitResponse(
                    year,
                    month,
                    revenue,
                    cogs,
                    grossProfit,
                    GrossProfitMath.MarginPercent(revenue, grossProfit),
                    days.Sum(d => d.SalesCount),
                    days.Sum(d => d.UnitsSold),
                    days);

                return new Result
                {
                    IsSuccess = true,
                    StatusCode = 200,
                    Status = "Success",
                    Message = "Monthly gross profit retrieved successfully",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating monthly gross profit for {Year}-{Month}", year, month);
                return new Result
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Status = "Error",
                    Message = "An error occurred while calculating monthly gross profit."
                };
            }
        }
    }
}
