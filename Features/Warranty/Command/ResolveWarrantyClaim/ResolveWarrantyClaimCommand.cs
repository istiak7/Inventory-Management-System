using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.ResolveWarrantyClaim
{
    public sealed record ResolveWarrantyClaimCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; init; }

        /// <summary>Repaired or Replaced. A claim that cannot be fixed goes through reject instead.</summary>
        public required string Resolution { get; init; }

        /// <summary>The unit handed over instead. Required for Replaced, ignored for Repaired.</summary>
        public string? ReplacementSerialNumber { get; init; }

        public string? Notes { get; init; }
    }
}
