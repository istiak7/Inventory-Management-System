using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetDailyGrossProfit
{
    /// <summary>Gross profit for one day. Date defaults to today; BranchId narrows to one branch.</summary>
    public sealed record GetDailyGrossProfitQuery(
        DateTime? Date = null,
        int? BranchId = null
    ) : IRequest<Result>;
}
