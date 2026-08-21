using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.ResolveWarrantyClaim
{
    public sealed record ResolveWarrantyClaimCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; init; }

        public required string Resolution { get; init; }

        public string? ReplacementSerialNumber { get; init; }

        public string? Notes { get; init; }
    }
}
