using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Purchases.Queries.GetAllPurchaseOrders
{
    public sealed record GetAllPurchaseOrdersQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Status = null,
        int? SupplierId = null,
        int? BranchId = null,
        string? PurchaseType = null,
        string? Search = null,
        DateTime? StartDate = null,
        DateTime? EndDate = null
    ) : IRequest<Result>;
}
