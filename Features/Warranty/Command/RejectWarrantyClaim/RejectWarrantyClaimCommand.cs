using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.RejectWarrantyClaim
{
    public sealed record RejectWarrantyClaimCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; init; }

        /// <summary>Why the shop is not fixing it — the customer will be told this, so it is required.</summary>
        public required string Reason { get; init; }
    }
}
