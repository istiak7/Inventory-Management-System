using Inventory_Management_System.Shared;
using MediatR;

namespace Inventory_Management_System.Features.Warranty.Command.StartWarrantyRepair
{
    public sealed record StartWarrantyRepairCommand : IRequest<Result>
    {
        public int WarrantyClaimId { get; init; }

        /// <summary>Who picked the job up. Overwrites whatever intake guessed, when supplied.</summary>
        public string? TechnicianName { get; init; }
    }
}
