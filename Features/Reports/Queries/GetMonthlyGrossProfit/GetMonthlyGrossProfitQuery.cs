using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetMonthlyGrossProfit
{
    /// <summary>Gross profit for one month. Year/Month default to the current month.</summary>
    public sealed record GetMonthlyGrossProfitQuery(
        int? Year = null,
        int? Month = null,
        int? BranchId = null
    ) : IRequest<Result>;
}
