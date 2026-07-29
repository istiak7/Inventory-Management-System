using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetAllStock
{
    public sealed record GetAllStockQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? BranchId = null,
        int? ProductVariantId = null,
        string? Search = null,               // matches SKU or product name
        int? LowStockThreshold = null        // when set, only rows with CurrentStock <= threshold
    ) : IRequest<Result>;
}
