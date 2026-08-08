using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.DeliverWarrantyClaim
{
    public sealed record DeliverWarrantyClaimCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; init; }
        public string? Notes { get; init; }
    }
}
