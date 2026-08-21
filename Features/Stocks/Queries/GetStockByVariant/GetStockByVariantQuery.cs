using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Stocks.Queries.GetStockByVariant
{
    public sealed record GetStockByVariantQuery(int ProductVariantId) : IRequest<Result>;
}
