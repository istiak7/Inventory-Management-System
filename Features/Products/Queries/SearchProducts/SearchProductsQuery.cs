using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Products.Queries.SearchProducts
{
    public sealed record SearchProductsQuery(
        string? Term = null,
        int? ProductId = null,
        int? BranchId = null,
        bool? IsSerialized = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        int PageNumber = 1,
        int PageSize = 20) : IRequest<Result>;
}
