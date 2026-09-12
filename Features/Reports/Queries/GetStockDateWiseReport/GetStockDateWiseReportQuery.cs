using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Reports.Queries.GetStockDateWiseReport
{
    public sealed record GetStockDateWiseReportQuery(
        int PageNumber = 1,
        int PageSize = 20,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        int? BranchId = null,
        int? ProductVariantId = null,
        int? ProductId = null,
        int? CategoryId = null,
        int? SubCategoryId = null,
        string? Search = null,
        string? SortBy = null,
        bool SortDescending = false
    ) : IRequest<Result>;
}
