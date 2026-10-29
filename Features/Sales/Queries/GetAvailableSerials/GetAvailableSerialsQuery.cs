using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Sales.Queries.GetAvailableSerials
{
    // Serial numbers a seller can pick for one serialized variant at one branch.
    // BranchId is required so the list matches exactly what CreateSale will accept
    // (it looks a serial up by variant + branch + InStock).
    public sealed record GetAvailableSerialsQuery(
        int ProductVariantId,
        int BranchId,
        string? Search = null
    ) : IRequest<Result>;
}
