using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Inventory.Queries.GetAllInventoryTransactions
{
    public sealed record GetAllInventoryTransactionsQuery(
        int PageNumber = 1,
        int PageSize = 20,
        int? BranchId = null,
        int? ProductVariantId = null,
        string? TransactionType = null
    ) : IRequest<Result>;
}
