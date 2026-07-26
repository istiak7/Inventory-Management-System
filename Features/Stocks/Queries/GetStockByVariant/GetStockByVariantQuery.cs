using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariant
{
    // Stock rows for one variant across every branch (plain list, no pagination).
    public sealed record GetStockByVariantQuery(int ProductVariantId) : IRequest<Result>;
}
