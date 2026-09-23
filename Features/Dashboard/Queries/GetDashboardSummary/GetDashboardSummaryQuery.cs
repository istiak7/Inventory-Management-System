using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Dashboard.Queries.GetDashboardSummary
{
    /// <summary>
    /// Dashboard figures. Days = how many days the movement chart and top products cover.
    /// LowStockThreshold = a stock row at or below this (but above 0) counts as "low stock".
    /// </summary>
    public sealed record GetDashboardSummaryQuery(int Days = 30, int LowStockThreshold = 5) : IRequest<Result>;
}
