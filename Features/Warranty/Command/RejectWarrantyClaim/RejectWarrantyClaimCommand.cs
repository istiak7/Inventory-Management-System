using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.RejectWarrantyClaim
{
    public sealed record RejectWarrantyClaimCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; init; }

        public required string Reason { get; init; }
    }
}
