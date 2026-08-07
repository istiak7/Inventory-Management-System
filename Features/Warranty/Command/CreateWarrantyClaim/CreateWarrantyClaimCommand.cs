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

        /// <summary>
        /// Branch that physically received the unit. Left null it falls back to the branch the
        /// serial currently belongs to — which is also the stock a replacement would be drawn from.
        /// </summary>
        public int? BranchId { get; init; }
    }
}
