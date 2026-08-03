using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Transfers.Queries.GetAllStockTransfers
{
    public sealed record GetAllStockTransfersQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? SourceBranchId = null,
        int? DestinationBranchId = null,
        string? Status = null,
        string? Search = null
    ) : IRequest<Result>;
}
