using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.CreateWarrantyClaim
{
    public sealed record CreateWarrantyClaimCommand : IRequest<Result>
    {
        public required string SerialNumber { get; init; }
        public required string DefectDescription { get; init; }
        public string? AccessoriesReceived { get; init; }
        public string? TechnicianName { get; init; }

        public int? BranchId { get; init; }
    }
}
