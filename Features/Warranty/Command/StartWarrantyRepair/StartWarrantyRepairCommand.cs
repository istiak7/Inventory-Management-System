using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.StartWarrantyRepair
{
    public sealed record StartWarrantyRepairCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; init; }

        public string? TechnicianName { get; init; }
    }
}
