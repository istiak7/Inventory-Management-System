using Inventory_Management_System.Shared;
using MediatR;
using Inventory_Management_System.Shared.CurrentUser;

namespace Inventory_Management_System.Features.Warranty.Command.CreateWarrantyClaim
{
    public sealed record CreateWarrantyClaimCommand : IRequest<Result>, IBranchScopedRequest
    {
        // No branch = the branch the unit is at (staff can only see units of their own branch).
        public bool IsAllowedForBranch(int userBranchId) => BranchId is null || BranchId == userBranchId;

        public required string SerialNumber { get; init; }
        public required string DefectDescription { get; init; }
        public string? AccessoriesReceived { get; init; }
        public string? TechnicianName { get; init; }

        public int? BranchId { get; init; }
    }
}
