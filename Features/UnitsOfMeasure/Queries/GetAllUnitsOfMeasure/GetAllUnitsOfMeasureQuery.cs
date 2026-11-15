using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.UnitsOfMeasure.Queries.GetAllUnitsOfMeasure
{
    // Status: "all" (default), "active" or "inactive".
    public sealed record GetAllUnitsOfMeasureQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? Search = null,
        string? Status = null
    ) : IRequest<Result>;
}
