using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetReplacementCandidates
{
    public sealed record GetReplacementCandidatesQuery(
        int WarrantyClaimId,
        string? Search = null
    ) : IRequest<Result>;
}
