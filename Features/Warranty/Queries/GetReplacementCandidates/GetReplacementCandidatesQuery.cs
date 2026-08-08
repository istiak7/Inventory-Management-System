using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Queries.GetReplacementCandidates
{
    /// <summary>
    /// Asked in the context of a claim rather than by (variant, branch): the claim already knows
    /// both, so the picker cannot offer a unit the resolve handler would then refuse.
    /// </summary>
    public sealed record GetReplacementCandidatesQuery(
        int WarrantyClaimId,
        string? Search = null
    ) : IRequest<Result>;
}
