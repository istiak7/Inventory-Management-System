using Inventory_Management_System.Shared;
using MediatR;
using System.Text.Json.Serialization;

namespace Inventory_Management_System.Features.Warranty.Command.UpdateWarrantyClaim
{
    // Corrects what was written down at intake. The unit, sale and customer stay fixed: to claim
    // for a different unit, delete the claim and open a new one.
    public sealed record UpdateWarrantyClaimCommand : IRequest<Result>
    {
        [JsonIgnore] public int Id { get; set; }
        public required string DefectDescription { get; init; }
        public string? AccessoriesReceived { get; init; }
        public string? TechnicianName { get; init; }
    }
}
